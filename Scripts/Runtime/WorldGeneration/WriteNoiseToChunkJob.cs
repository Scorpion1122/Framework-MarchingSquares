using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace Thijs.Framework.MarchingSquares
{
    [BurstCompile]
    public struct WriteNoiseToChunkJob : IJobParallelFor
    {
        [ReadOnly] public float tileSize;
        [ReadOnly] public int resolution;
        [ReadOnly] public float noiseCutOff;

        [ReadOnly] public FillType fillType;

        [ReadOnly] public NativeArray<float> noise;

        public NativeArray<FillType> fillTypes;
        public NativeArray<float2> offsets;
        public NativeArray<float2> normalsX;
        public NativeArray<float2> normalsY;

        public void Execute(int index)
        {
            if (fillTypes[index] == FillType.None)
                return;

            float currentNoise = GetNoise(index);
            float topRightNoise = GetNoise(index + resolution + 1);
            float topNoise = GetNoise(index + resolution);
            float rightNoise = GetNoise(index + 1);

            bool fillCurrent = currentNoise >= noiseCutOff;
            bool fillTop = topNoise >= noiseCutOff;
            bool fillRight = rightNoise >= noiseCutOff;

            float2 normal = GetNormal(currentNoise, topNoise, topRightNoise, rightNoise);

            if (fillCurrent)
                fillTypes[index] = fillType;

            float2 offset = offsets[index];
            if (fillCurrent != fillTop)
            {
                offset.y = GetIntersection(currentNoise, topNoise) * tileSize;
                normalsY[index] = normal;
            }

            if (fillCurrent != fillRight)
            {
                offset.x = GetIntersection(currentNoise, rightNoise) * tileSize;
                normalsX[index] = normal;
            }
            offsets[index] = offset;
        }

        private float GetIntersection(float noiseValue, float noiseValueTwo)
        {
            if (noiseValue == 0f && noiseValueTwo == 0f)
                return 0f;

            float diffToTwo = math.abs(noiseValue - noiseValueTwo);
            float diffToCutOff = math.abs(noiseValue - noiseCutOff);
            return diffToCutOff/diffToTwo;
        }

        private float2 GetNormal(float current, float top, float topRight, float right)
        {
            float total = current + top + topRight + right;
            float2 result = new float2(-0.5f, -0.5f) * current;
            result += new float2(-0.5f, 0.5f) * top;
            result += new float2(0.5f, 0.5f) * topRight;
            result += new float2(0.5f, -0.5f) * right;
            result /= total;
            return math.normalize(result);
        }

        private FillType GetFillType(int index)
        {
            if (index >= fillTypes.Length)
                return FillType.None;
            return fillTypes[index];
        }

        private float GetNoise(int index)
        {
            if (index >= noise.Length)
                return 0f;
            return noise[index];
        }
    }
}
