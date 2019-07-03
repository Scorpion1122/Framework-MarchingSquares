namespace Thijs.Framework.MarchingSquares
{
    public struct Voxel
    {
        public static readonly Voxel EMPTY = new Voxel();

        public FillType fill;
        public float offsetX;
        public float offsetY;
    }
}
