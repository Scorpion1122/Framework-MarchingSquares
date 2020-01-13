using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

namespace Thijs.Framework.MarchingSquares
{
    [BurstCompile]
    public struct NoiseGenerationJob : IJobParallelFor
    {
        [ReadOnly] public float tileSize;
        [ReadOnly] public int resolution;
        [ReadOnly] public float2 origin;

        [ReadOnly] public NativeArray<NoiseSettings> noiseInput;
        [WriteOnly] public NativeArray<float> noiseOutput;

        public void Execute(int index)
        {
            float2 position = VoxelUtility.IndexToPosition(index, resolution, tileSize);
            position += origin;

            float value = 0f;
            for (int i = 0; i < noiseInput.Length; i++)
                value += NoiseUtility.GetNoiseValue(position, noiseInput[i]);
            value /= noiseInput.Length;

            noiseOutput[index] = value;
        }
    }
}
