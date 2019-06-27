using Unity.Mathematics;

public static class ChunkUtility
{
    public static int2 IndexToIndex2(int index, int gridResolution)
    {
        int x = (int)math.floor(index % gridResolution);
        int y = (int)math.floor((index - x) / gridResolution);
        return new int2(x, y);
    }

    public static int Index2ToIndex(int2 index, int gridResolution)
    {
        return index.x + index.y * gridResolution;
    }

    public static float2 GetChunkOrigin(int index, int gridResolution, float chunkSize)
    {
        int2 index2 = IndexToIndex2(index, gridResolution);
        float2 originOffset = GetGridOrigin(gridResolution, chunkSize);
        return new float2(index2.x * chunkSize, index2.y * chunkSize) + originOffset;
    }

    public static float2 GetGridOrigin(int gridResolution, float chunkSize)
    {
        return new float2(-0.5f, -0.5f) * gridResolution * chunkSize;
    }

    public static int PositionToChunkIndex(float2 position, int gridResolution, float chunkSize)
    {
        int x = (int)math.floor(position.x / chunkSize);
        int y = (int)math.floor(position.y / chunkSize);
        int2 index2 = new int2(x, y);
        return Index2ToIndex(index2, gridResolution);
    }

    public static bool IsChunkIndexValid(int index, int gridResolution)
    {
        return index >= 0 && index < (gridResolution * gridResolution);
    }
}
