using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [ExecuteInEditMode]
    public class TileTerrainRenderer : TileTerrainComponent
    {
        private void OnEnable()
        {
            TileTerrain.OnChunkInitialized += OnChunkInitialized;
        }

        private void OnChunkInitialized(int2 chunkIndex, ChunkData chunkData)
        {
            GameObject gameObject = new GameObject("Chunk Renderer");
            gameObject.hideFlags = HideFlags.DontSave;
            gameObject.transform.SetParent(transform);
            gameObject.transform.position = transform.TransformPoint(chunkData.origin.x, chunkData.origin.y, 0f);

            ChunkRenderer chunkRenderer = gameObject.AddComponent<ChunkRenderer>();
            chunkData.dependencies.Add(chunkRenderer);
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInitialized -= OnChunkInitialized;
        }
    }
}