﻿using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Thijs.Framework.MarchingSquares
{
    [BurstCompile]
    public struct HeightGenerationJob : IJobParallelFor
    {
        [ReadOnly] public float tileSize;
        [ReadOnly] public int resolution;
        [ReadOnly] public float2 origin;

        [ReadOnly] public FillType fillType;

        [ReadOnly] public float heightOffset;
        [ReadOnly] public float heightScale;
        [ReadOnly] public NoiseSettings noiseSettings;

        public NativeArray<FillType> fillTypes;
        public NativeArray<float2> offsets;
        public NativeArray<float2> normalsX;
        public NativeArray<float2> normalsY;

        public void Execute(int index)
        {
            float2 position = VoxelUtility.IndexToPosition(index, resolution, tileSize) + origin;
            float height = GetHeightValue(position);
            float nextHeight = GetHeightValue(position + new float2(tileSize, 0f));
            float2 slopeNormal = math.normalize(new float2((height - nextHeight), tileSize));

            float yOffset = height - position.y;
            if (yOffset < 0f)
                fillTypes[index] = FillType.None;
            else
                fillTypes[index] = fillType;
            
            float2 offset = offsets[index];
            if (yOffset > 0f && yOffset < tileSize)
            {
                offset.y = yOffset;
                normalsY[index] = slopeNormal;
            }
            

            if (math.sign(yOffset) != math.sign(nextHeight - position.y))
            {
                float2 adjacentNormalized = math.normalize(new float2(0f, -yOffset));
                float2 obliqueNormalized = math.normalize(new float2(tileSize, nextHeight - height));
                float angle = math.acos(math.dot(obliqueNormalized, adjacentNormalized));

                float xOffset = math.abs(math.tan(angle) * -yOffset);

                if (xOffset > 0f && xOffset < tileSize)
                {
                    offset.x = xOffset;
                    normalsX[index] = slopeNormal;
                }
            }
            offsets[index] = offset;
        }

        private float GetHeightValue(float2 position)
        {
            float noise = NoiseUtility.GetNoiseValue(new float2(position.x, 0f), noiseSettings);
            return noise * heightScale + heightOffset;
        }
    }
}
