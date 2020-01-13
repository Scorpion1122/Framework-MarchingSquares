using Unity.Mathematics;

namespace Thijs.Framework.MarchingSquares
{
    public static class NoiseUtility
    {
        public static float GetNoiseValue(float2 position, NoiseSettings settings)
        {
            position += settings.offset;

            float value = 0f;

            float noise = GetNoiseValue(position * settings.frequency, settings.type);
            if (settings.ridges)
                noise = GetRigdeNoiseValue(noise);
            value += noise;

            if (settings.frequencyTwo != 0f)
            {
                noise = GetNoiseValue(position * settings.frequencyTwo, settings.type) * 0.5f;
                if (settings.ridges)
                    noise = GetRigdeNoiseValue(noise);
                value += noise;
            }

            if (settings.frequencyThree != 0f)
            {
                noise = GetNoiseValue(position * settings.frequencyThree, settings.type) * 0.25f;
                if (settings.ridges)
                    noise = GetRigdeNoiseValue(noise);
                value += noise;
            }


            if (settings.invert)
                value = 1f - value;

            return value;
        }

        public static float GetRigdeNoiseValue(float value)
        {
            return 2f * (0.5f - math.abs(0.5f - value));
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
            //return math.round(value * 8) / 8; //Terrasing
            return value;
        }
    }
}
