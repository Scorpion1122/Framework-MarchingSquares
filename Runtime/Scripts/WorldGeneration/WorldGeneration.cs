using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldGeneration : MonoBehaviour
{
    [SerializeField] private float noiseScale = 5f;

    public void GenerateChunkData(VoxelGrid grid, ChunkData chunk)
    {
        float maxHeight = grid.Size * grid.Resolution;

        GenerateHeight(grid, chunk, FillType.TypeOne, new float2(0.1f, 0.15f) * maxHeight);
        //GenerateHeight(grid, chunk, FillType.TypeTwo, new float2(0.5f, 0.75f) * maxHeight);
    }

    private void GenerateHeight(VoxelGrid grid, ChunkData chunk, FillType fillType, float2 heightRange)
    {
        float random = Random.Range(0f, 1f);

        for (int i = 0; i < grid.Resolution; i++)
        {
            float x = i * grid.Size;
            float noise = Mathf.PerlinNoise(x * random * noiseScale, 0f);
            float y = Mathf.Lerp(heightRange.x, heightRange.y, noise);
            SetHeight(grid, chunk, fillType, i, y);
        }
    }

    private void SetHeight(VoxelGrid grid, ChunkData chunk, FillType fillType, int x, float height)
    {
        for (int i = 0; i < grid.Resolution; i++)
        {
            int2 index2 = new int2(x, i);
            int index = VoxelUtility.Index2ToIndex(index2, grid.Resolution);
            float2 position = VoxelUtility.IndexToPosition(index, grid.Resolution, grid.Size);

            if (position.y > height)
                return;

            chunk.fillTypes[index] = fillType;
            float difference = height - position.y;
            if (difference < grid.Size)
            {
                float2 offset = chunk.offsets[index];
                offset.y = difference;
                chunk.offsets[index] = offset;
            }
        }
    }
}
