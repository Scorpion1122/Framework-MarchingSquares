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


        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInstantiated -= OnChunkInitialized;
        }

        public JobHandle ScheduleChunkJob(TileTerrain grid, ChunkData chunkData, JobHandle dependency)
        {
            Rect bounds = chunkData.GetBounds();

            for (int i = 0; i < tilemaps.Length; i++)
            {
                Tilemap tilemap = tilemaps[i];
                BoundsInt boundsInt = tilemap.cellBounds;
                Rect tilemapBounds = new Rect(boundsInt.min.x, boundsInt.min.y, boundsInt.size.x, boundsInt.size.y);

                if (!bounds.Overlaps(tilemapBounds))
                    continue;


            }

                //for (int i = 0; i < tilemaps.Length; i++)
                //{
                //    Tilemap tilemap = tilemaps[i];
                //    int count = tilemap.GetUsedTilesNonAlloc(tiles);

                //    tilemap.GetTilesBlock
                //    for (int j = 0; j < count; j++)
                //    {
                //        TileBase tile = tiles[j];
                //    }

                //}

                //UnityEngine.Tilemaps.Tilemap map;
                //map.GetTilesBlock();
                //map.cellSize;

                return dependency;
        }

        public void OnJobCompleted(ChunkData chunkData)
        {
            chunkData.dependencies.Remove(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < tilemaps.Length; i++)
            {
                Tilemap tilemap = tilemaps[i];
                BoundsInt boundsInt = tilemap.cellBounds;

                Gizmos.DrawCube(boundsInt.center, boundsInt.size);
            }
            Gizmos.color = Color.white;
        }
    }
}
