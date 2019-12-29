using System;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Thijs.Framework.MarchingSquares
{
    public class TilemapBlocker : TileTerrainComponent, IChunkJobDependency
    {
        private static TileBase[] tiles = new TileBase[1000];

        [SerializeField] private Tilemap[] tilemaps;

        public bool IsBlocking => true;

        private void OnEnable()
        {
            TileTerrain.OnChunkInstantiated += OnChunkInitialized;
        }

        private void OnChunkInitialized(int2 index, ChunkData chunkData)
        {
            chunkData.dependencies.Add(this);
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInstantiated -= OnChunkInitialized;
        }

        public JobHandle ScheduleChunkJob(TileTerrain grid, ChunkData chunkData, JobHandle dependency)
        {
            for (int i = 0; i < tilemaps.Length; i++)
            {
                Tilemap tilemap = tilemaps[i];
                int count = tilemap.GetUsedTilesNonAlloc(tiles);
            }

            //UnityEngine.Tilemaps.Tilemap map;
            //map.GetTilesBlock();
            //map.cellSize;
        }

        public void OnJobCompleted(ChunkData chunkData)
        {
            chunkData.dependencies.Remove(this);
        }
    }
}
