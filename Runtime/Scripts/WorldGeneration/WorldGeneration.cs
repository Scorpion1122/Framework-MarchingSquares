using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Thijs.Framework.MarchingSquares
{
    public class WorldGeneration : MonoBehaviour
    {
        [SerializeField] private float noiseScale = 5f;

        public void GenerateChunkData(TileTerrain grid, ChunkData chunk)
        {
            float maxHeight = grid.TileSize * grid.ChunkResolution;

            GenerateHeight(grid, chunk, FillType.TypeOne, new float2(0.1f, 0.15f) * maxHeight);
            //GenerateHeight(grid, chunk, FillType.TypeTwo, new float2(0.5f, 0.75f) * maxHeight);
        }

        private void GenerateHeight(TileTerrain grid, ChunkData chunk, FillType fillType, float2 heightRange)
        {
            float random = Random.Range(0f, 1f);

            for (int i = 0; i < grid.ChunkResolution; i++)
            {
                float x = i * grid.TileSize + chunk.origin.x;
                float noise = Mathf.PerlinNoise(x * random * noiseScale, 0f);
                float y = Mathf.Lerp(heightRange.x, heightRange.y, noise);
                //SetHeight(grid, chunk, fillType, i, y);
                SetHeight(grid, chunk, fillType, i, heightRange.y);
            }
        }

        private void SetHeight(TileTerrain grid, ChunkData chunk, FillType fillType, int x, float height)
        {
            for (int i = 0; i < grid.ChunkResolution; i++)
            {
                int2 index2 = new int2(x, i);
                int index = VoxelUtility.Index2ToIndex(index2, grid.ChunkResolution);
                float2 position = VoxelUtility.IndexToPosition(index, grid.ChunkResolution, grid.TileSize) + chunk.origin;

                if (position.y > height)
                    return;

                chunk.fillTypes[index] = fillType;
                float difference = height - position.y;
                if (difference < grid.TileSize)
                {
                    float2 offset = chunk.offsets[index];
                    offset.y = difference;
                    chunk.offsets[index] = offset;
                }
            }
        }
    }
}
