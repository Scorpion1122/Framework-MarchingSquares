using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [ExecuteInEditMode]
    public class TileTerrainRenderer : TileTerrainComponent
    {
        private void OnEnable()
        {
            TileTerrain.OnChunkInstantiated += OnChunkInitialized;
            TileTerrain.OnChunkDestroyed += OnChunkDestroyed;
        }

        private void OnChunkInitialized(int2 chunkIndex, ChunkData chunkData)
        {
            GameObject gameObject = new GameObject("Chunk Renderer");
            gameObject.hideFlags = HideFlags.DontSave;
            gameObject.transform.SetParent(transform);
            gameObject.transform.position = transform.TransformPoint(chunkData.Origin.x, chunkData.Origin.y, 0f);

            ChunkRenderer chunkRenderer = gameObject.AddComponent<ChunkRenderer>();
            chunkData.dependencies.Add(chunkRenderer);
        }

        private void OnChunkDestroyed(int2 chunkIndex, ChunkData chunkData)
        {
            for (int i = chunkData.dependencies.Count - 1; i >= 0; i--)
            {
                if (chunkData.dependencies[i] is ChunkRenderer renderer)
                {
                    DestroyImmediate(renderer.gameObject);
                    chunkData.dependencies.Remove(renderer);
                }
            }
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInstantiated -= OnChunkInitialized;
            TileTerrain.OnChunkDestroyed -= OnChunkDestroyed;
        }
    }
}