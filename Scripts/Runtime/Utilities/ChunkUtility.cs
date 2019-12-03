using Unity.Mathematics;

namespace Thijs.Framework.MarchingSquares
{
    public static class ChunkUtility
    {
        public static int2 IndexToIndex2(int index, int gridResolution)
        {
            int x = (int) math.floor(index % gridResolution);
            int y = (int) math.floor((index - x) / gridResolution);
            return new int2(x, y);
        }

        public static int Index2ToIndex(int2 index, int gridResolution)
        {
            return index.x + index.y * gridResolution;
        }

        public static float2 GetChunkOrigin(int index, int gridResolution, float chunkSize)
        {
            int2 index2 = IndexToIndex2(index, gridResolution);
            return GetChunkOrigin(index2, chunkSize);
        }

        public static float2 GetChunkOrigin(int2 index, float chunkSize)
        {
            return new float2(index.x * chunkSize, index.y * chunkSize);
        }

        public static int2 PositionToChunkIndex(float2 position, float chunkSize)
        {
            int x = (int) math.floor(position.x / chunkSize);
            int y = (int) math.floor(position.y / chunkSize);
            int2 index2 = new int2(x, y);
            return index2;
        }

        public static bool IsChunkIndexValid(int index, int gridResolution)
        {
            return index >= 0 && index < (gridResolution * gridResolution);
        }
    }
}
