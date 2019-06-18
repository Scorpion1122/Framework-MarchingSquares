using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ModifyOffsetsJob : IJobParallelFor
{
    [ReadOnly] public float size;
    [ReadOnly] public int resolution;
    [ReadOnly] public NativeList<GridModification> modifiers;
    [ReadOnly] public NativeArray<FillType> fillTypes;
    public NativeArray<float2> offsets;

    public void Execute(int index)
    {
        if (ShouldZeroOutOffsets(index))
        {
            offsets[index] = float2.zero;
            return;
        }

        for (int i = 0; i < modifiers.Length; i++)
        {
            GridModification modifier = modifiers[i];
            switch (modifier.modifierType)
            {
                case ModifierType.Circle:
                    RunCircleModifier(index, modifier);
                    break;
                case ModifierType.Square:
                    RunSquareModifier(index, modifier);
                    break;
            }
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

    private void RunCircleModifier(int index, GridModification modifier)
    {
        float2 offset = offsets[index];

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

        if (topFillType == currentFillType)
        {
            offset.y = 0f;
        }
        else if (withinCircle && !topWithinCircle)
        {
            float newOffset = intersectY - difference.y;
            newOffset = math.clamp(newOffset, 0, size);
            offset.y = math.max(newOffset, offset.y);
        }
        else if (!withinCircle && topWithinCircle)
        {
            float newOffset = (intersectY + difference.y) * -1;
            newOffset = math.clamp(newOffset, 0, size);
            if (offset.y == 0 || offset.y > newOffset)
                offset.y = newOffset;
        }

        if (rightFillType == currentFillType)
        {
            offset.x = 0;
        }
        else if (withinCircle && !rightWithinCircle)
        {
            float newOffset = intersectX - difference.x;
            newOffset = math.clamp(newOffset, 0, size);
            offset.x = math.max(newOffset, offset.x);
        }
        else if (!withinCircle && rightWithinCircle)
        {
            float newOffset = (intersectX + difference.x) * -1;
            newOffset = math.clamp(newOffset, 0, size);
            if (offset.x == 0 || offset.x > newOffset)
                offset.x = newOffset;
        }

        offsets[index] = offset;
    }

    private void RunSquareModifier(int index, GridModification modifier)
    {
        float2 offset = offsets[index];

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

            //Both are of the same type, so zero it out
            if (currentFillType == rightFillType)
            {
                offset.x = 0f;
            }
            //Current within modifier, right not in modifier
            else if (withinLength && !rightWithinLength)
            {
                float newOffset = max.x - position.x;
                newOffset = math.clamp(newOffset, 0f, size);
                offset.x = math.max(offset.x, newOffset);
            }
            //Current outside modifier, right inside modifier
            else if (!withinLength && rightWithinLength)
            {
                float newOffset = min.x - position.x;
                newOffset = math.clamp(newOffset, 0f, size);
                if (offset.x == 0f || offset.x > newOffset)
                    offset.x = newOffset;
            }
        }

        if (withinLength)
        {
            FillType topFillType = VoxelUtility.GetNeightbour(fillTypes, index + resolution);
            float2 topPosition = VoxelUtility.IndexToPosition(index + resolution, resolution, size);
            bool topWithinHeight = topPosition.y >= min.y && topPosition.y <= max.y;

            //Both are of the same type, so zero it out
            if (currentFillType == topFillType)
            {
                offset.y = 0f;
            }
            //Current within modifier, top not in modifier
            else if (withinHeight && !topWithinHeight)
            {
                float newOffset = max.y - position.y;
                newOffset = math.clamp(newOffset, 0f, size);
                offset.y = math.max(offset.y, newOffset);
            }
            //Current outside modifier, top inside modifier
            else if (!withinHeight && topWithinHeight)
            {
                float newOffset = min.y - position.y;
                newOffset = math.clamp(newOffset, 0f, size);
                if (offset.y == 0f || offset.y > newOffset)
                    offset.y = newOffset;
            }
        }

        offsets[index] = offset;
    }
}
