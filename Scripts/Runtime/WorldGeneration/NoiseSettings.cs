using System;

namespace Thijs.Framework.MarchingSquares
{
    [Serializable]
    public struct NoiseSettings
    {
        public NoiseType type;
        public float frequency;
        public int offset;
        public bool invert;
    }
}
