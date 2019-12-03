namespace Thijs.Framework.MarchingSquares
{
    public enum ModifierType
    {
        Always, // Paint everything
        // TODO : Make work with the offsets
        Replace, //Only modify when fill type is NOT none
        Fill, // Only modify when fill type IS none
    }
}