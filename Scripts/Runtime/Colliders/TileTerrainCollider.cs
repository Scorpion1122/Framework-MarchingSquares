using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public class TileTerrainCollider : TileTerrainComponent
    {
        private void OnEnable()
        {
            TileTerrain.OnChunkInitialized += OnChunkInitialized;
        }

        private void OnChunkInitialized(int2 chunkIndex, ChunkData chunkData)
        {
            GameObject gameObject = new GameObject("Chunk Collider");
            gameObject.hideFlags = HideFlags.DontSave;
            gameObject.transform.SetParent(transform);
            gameObject.transform.position = transform.TransformPoint(chunkData.origin.x, chunkData.origin.y, 0f);
            
            ChunkCollider chunkCollider = gameObject.AddComponent<ChunkCollider>();
            chunkData.dependencies.Add(chunkCollider);
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInitialized -= OnChunkInitialized;
        }
    }
}