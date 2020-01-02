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
                value += GetNoiseValue(position, noiseInput[i]);
            value /= noiseInput.Length;

            noiseOutput[index] = value;
        }

        private float GetNoiseValue(float2 position, NoiseSettings settings)
        {
            position *= settings.frequency;
            position += settings.offset;

            float value = 0f;
            switch (settings.type)
            {
                case NoiseType.Perlin:
                    value = math.unlerp(-1, 1f, noise.cnoise(position));
                    break;

                case NoiseType.Simplex:
                    value = math.unlerp(-1, 1f, noise.snoise(position));
                    break;

                case NoiseType.CellulerF1:
                    value = noise.cellular(position).x;
                    break;

                case NoiseType.CellulerF2:
                    value = noise.cellular(position).y;
                    break;
            }

            if (settings.invert)
                value = 1f - value;

            return value;
        }
    }
}
