using Unity.Mathematics;

namespace Thijs.Framework.MarchingSquares
{
    public static class NoiseUtility
    {
        public static float GetNoiseValue(float2 position, NoiseSettings settings)
        {
            position += settings.offset;

            float value = GetNoiseValue(position * settings.frequency, settings.type);
            if (settings.frequencyTwo != 0f)
                value += GetNoiseValue(position * settings.frequencyTwo, settings.type) * 0.5f;
            if (settings.frequencyThree!= 0f)
                value += GetNoiseValue(position * settings.frequencyThree, settings.type) * 0.25f;

            if (settings.invert)
                value = 1f - value;

            return value;
        }

        public static float GetNoiseValue(float2 position, NoiseType type)
        {
            float value = 0f;
            switch (type)
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
            return value;
        }
    }
}
