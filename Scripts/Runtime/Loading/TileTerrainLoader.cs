using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Thijs.Framework.MarchingSquares.Loading
{
    [ExecuteInEditMode]
    public class TileTerrainLoader : TileTerrainComponent
    {
        [SerializeField] private int padding = 1;

        private List<Camera> cameras = new List<Camera>();

        private List<int2> loadedChunks = new List<int2>();
        private List<int2> chunks = new List<int2>();

        private void OnEnable()
        {
            TileTerrain.OnChunkInstantiated += OnChunkInitialized;
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInstantiated -= OnChunkInitialized;
        }

        private void OnChunkInitialized(int2 chunkIndex, ChunkData chunkData)
        {
            loadedChunks.Add(chunkIndex);
        }

        private void Update()
        {
            cameras.Clear();
            CameraUtility.GetActiveCameras(ref cameras);
            
            chunks.Clear();
            for (int i = 0; i < cameras.Count; i++)
                CameraUtility.AddChunkRangeInCameraView(cameras[i], padding, TileTerrain.ChunkSize, ref chunks);
            //chunks.Add(int2.zero);

            for (int i = 0; i < chunks.Count; i++)
            {
                if (!TileTerrain.IsChunkActive(chunks[i]))
                {
                    TileTerrain.LoadChunk(chunks[i]);
                }
            }

            for (int i = 0; i < loadedChunks.Count; i++)
            {
                if (!chunks.Contains(loadedChunks[i]))
                {
                    TileTerrain.UnloadChunk(loadedChunks[i]);
                    loadedChunks.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}