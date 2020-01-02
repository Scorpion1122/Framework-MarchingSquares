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
            float topNoise = GetNoise(index + resolution);
            float rightNoise = GetNoise(index + 1);

            bool fillCurrent = currentNoise >= noiseCutOff;
            bool fillTop = topNoise >= noiseCutOff;
            bool fillRight = rightNoise >= noiseCutOff;

            if (fillCurrent)
                fillTypes[index] = fillType;

            float2 offset = offsets[index];
            if (fillCurrent != fillTop)
                offset.y = GetIntersection(currentNoise, topNoise) * tileSize;

            if (fillCurrent != fillRight)
                offset.x = GetIntersection(currentNoise, rightNoise) * tileSize;
            offsets[index] = offset;
        }

        private float GetIntersection(float noiseValue, float noiseValueTwo)
        {
            if (noiseValue == 0f && noiseValueTwo == 0f)
                return 0f;
            return noiseValue / (noiseValue + noiseValueTwo);
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
