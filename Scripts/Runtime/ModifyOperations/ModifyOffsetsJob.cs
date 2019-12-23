using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Thijs.Framework.MarchingSquares
{
    [BurstCompile]
    public struct ModifyOffsetsJob : IJobParallelFor
    {
        [ReadOnly] public float size;
        [ReadOnly] public int resolution;
        [ReadOnly] public NativeList<GridModification> modifiers;
        [ReadOnly] public NativeArray<FillType> fillTypes;
        public NativeArray<float2> offsets;
        public NativeArray<float2> normalsX;
        public NativeArray<float2> normalsY;

        public void Execute(int index)
        {
            if (ShouldZeroOutOffsets(index))
            {
                offsets[index] = float2.zero;
                normalsX[index] = float2.zero;
                normalsY[index] = float2.zero;
                return;
            }

            for (int i = 0; i < modifiers.Length; i++)
            {
                GridModification modifier = modifiers[i];
                switch (modifier.ModifierShape)
                {
                    case ModifierShape.Circle:
                        RunCircleModifier(index, modifier);
                        break;
                    case ModifierShape.Square:
                        RunSquareModifier(index, modifier);
                        break;
                }
            }

            UpdateOffsetsForNeighbourChunk(index);
        }
        
        private bool CanChangeOffsets(FillType currentFillType, FillType otherFillType, ModifierType modifierType)
        {
            switch (modifierType)
            {
                case ModifierType.Always:
                    return true;
                case ModifierType.Replace:
                    return currentFillType != otherFillType && otherFillType != FillType.None;
                case ModifierType.Fill:
                    return currentFillType == otherFillType || otherFillType == FillType.None || currentFillType == FillType.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifierType), modifierType, null);
            }
        }

        private bool ShouldZeroOutOffsets(int index)
        {
            FillType currentFillType = fillTypes[index];
            FillType topFillType = VoxelUtility.GetNeightbour(fillTypes, index + resolution);
            FillType rightFillType = VoxelUtility.GetNeightbour(fillTypes, index + 1);

            if (currentFillType == topFillType && currentFillType == rightFillType)
            {
                return true;
            }

            return false;
        }

        private void UpdateOffsetsForNeighbourChunk(int index)
        {
            float2 offset = offsets[index];

            int2 index2 = VoxelUtility.IndexToIndex2(index, resolution);
            if (index2.x == resolution - 1)
            {
                offset.x = 0f;
            }

            if (index2.y == resolution - 1)
            {
                offset.y = 0f;
            }

            offsets[index] = offset;
        }

        private void RunCircleModifier(int index, GridModification modifier)
        {
            float2 offset = offsets[index];
            float2 normalX = normalsX[index];
            float2 normalY = normalsY[index];

            float2 position = VoxelUtility.IndexToPosition(index, resolution, size);
            float2 topPosition = VoxelUtility.IndexToPosition(index + resolution, resolution, size);
            float2 rightPosition = VoxelUtility.IndexToPosition(index + 1, resolution, size);

            float2 difference = position - modifier.position;
            bool withinCircle = math.length(difference) <= modifier.size;
            bool topWithinCircle = math.length(topPosition - modifier.position) <= modifier.size;
            bool rightWithinCircle = math.length(rightPosition - modifier.position) <= modifier.size;

            FillType currentFillType = fillTypes[index];
            FillType topFillType = VoxelUtility.GetNeightbour(fillTypes, index + resolution);
            FillType rightFillType = VoxelUtility.GetNeightbour(fillTypes, index + 1);

            float radius2 = math.pow(modifier.size, 2);
            float intersectX = math.sqrt(radius2 - math.pow(difference.y, 2));
            float intersectY = math.sqrt(radius2 - math.pow(difference.x, 2));

            bool canModifyY = CanChangeOffsets(currentFillType, topFillType, modifier.modifierType);
            bool canModifyX = CanChangeOffsets(currentFillType, rightFillType, modifier.modifierType);

            if (topFillType == currentFillType)
            {
                offset.y = 0f;
                normalY = float2.zero;
            }
            else if (canModifyY && withinCircle && !topWithinCircle)
            {
                float newOffset = intersectY - difference.y;
                newOffset = math.clamp(newOffset, 0, size);
                if (newOffset > offset.y)
                {
                    offset.y = newOffset;
                    normalY = math.normalize((position + new float2(0, offset.y)) - modifier.position);
                }
            }
            else if (canModifyY && !withinCircle && topWithinCircle)
            {
                float newOffset = (intersectY + difference.y) * -1;
                newOffset = math.clamp(newOffset, 0, size);
                if (offset.y == 0 || offset.y > newOffset)
                {
                    offset.y = newOffset;
                    normalY = math.normalize((position + new float2(0, offset.y)) - modifier.position);
                }
            }

            if (rightFillType == currentFillType)
            {
                offset.x = 0;
                normalX = float2.zero;
            }
            else if (canModifyX && withinCircle && !rightWithinCircle)
            {
                float newOffset = intersectX - difference.x;
                newOffset = math.clamp(newOffset, 0, size);
                if (newOffset > offset.x)
                {
                    offset.x = newOffset;
                    normalX = math.normalize((position + new float2(offset.x, 0)) - modifier.position);
                }
            }
            else if (canModifyX && !withinCircle && rightWithinCircle)
            {
                float newOffset = (intersectX + difference.x) * -1;
                newOffset = math.clamp(newOffset, 0, size);
                if (offset.x == 0 || offset.x > newOffset)
                {
                    offset.x = newOffset;
                    normalX = math.normalize((position + new float2(offset.x, 0)) - modifier.position);
                }
            }

            offsets[index] = offset;
            normalsX[index] = normalX;
            normalsY[index] = normalY;
        }

        private void RunSquareModifier(int index, GridModification modifier)
        {
            float2 offset = offsets[index];
            float2 normalX = normalsX[index];
            float2 normalY = normalsY[index];

            float2 min = new float2(modifier.position.x - modifier.size, modifier.position.y - modifier.size);
            float2 max = new float2(modifier.position.x + modifier.size, modifier.position.y + modifier.size);

            float2 position = VoxelUtility.IndexToPosition(index, resolution, size);
            FillType currentFillType = fillTypes[index];

            bool withinHeight = position.y >= min.y && position.y <= max.y;
            bool withinLength = position.x >= min.x && position.x <= max.x;

            //Update the offset on the X axis (length)
            if (withinHeight)
            {
                FillType rightFillType = VoxelUtility.GetNeightbour(fillTypes, index + 1);
                float2 rightPosition = VoxelUtility.IndexToPosition(index + 1, resolution, size);
                bool rightWithinLength = rightPosition.x >= min.x && rightPosition.x <= max.x;
                
                bool canModifyX = CanChangeOffsets(currentFillType, rightFillType, modifier.modifierType);

                //Both are of the same type, so zero it out
                if (currentFillType == rightFillType)
                {
                    offset.x = 0f;
                    normalX = float2.zero;
                }
                //Current within modifier, right not in modifier
                else if (canModifyX && withinLength && !rightWithinLength)
                {
                    float newOffset = max.x - position.x;
                    newOffset = math.clamp(newOffset, 0f, size);
                    if (newOffset > offset.x)
                    {
                        offset.x = newOffset;
                        normalX = new float2(1, 0f);
                    }
                }
                //Current outside modifier, right inside modifier
                else if (canModifyX && !withinLength && rightWithinLength)
                {
                    float newOffset = min.x - position.x;
                    newOffset = math.clamp(newOffset, 0f, size);
                    if (offset.x == 0f || offset.x > newOffset)
                    {
                        offset.x = newOffset;
                        normalX = new float2(-1, 0f);
                    }
                }
            }

            if (withinLength)
            {
                FillType topFillType = VoxelUtility.GetNeightbour(fillTypes, index + resolution);
                float2 topPosition = VoxelUtility.IndexToPosition(index + resolution, resolution, size);
                bool topWithinHeight = topPosition.y >= min.y && topPosition.y <= max.y;
                
                bool canModifyY = CanChangeOffsets(currentFillType, topFillType, modifier.modifierType);

                //Both are of the same type, so zero it out
                if (currentFillType == topFillType)
                {
                    offset.y = 0f;
                    normalY = float2.zero;
                }
                //Current within modifier, top not in modifier
                else if (canModifyY && withinHeight && !topWithinHeight)
                {
                    float newOffset = max.y - position.y;
                    newOffset = math.clamp(newOffset, 0f, size);
                    if (newOffset > offset.y)
                    {
                        offset.y = newOffset;
                        normalY = new float2(0, 1);
                    }
                }
                //Current outside modifier, top inside modifier
                else if (canModifyY && !withinHeight && topWithinHeight)
                {
                    float newOffset = min.y - position.y;
                    newOffset = math.clamp(newOffset, 0f, size);
                    if (offset.y == 0f || offset.y > newOffset)
                    {
                        offset.y = newOffset;
                        normalY = new float2(0, -1);
                    }
                }
            }

            offsets[index] = offset;
            normalsX[index] = normalX;
            normalsY[index] = normalY;
        }
    }
}
