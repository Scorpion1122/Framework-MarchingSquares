using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Thijs.Framework.MarchingSquares
{
    [ExecuteInEditMode]
    [DependsOn(typeof(WorldGeneration))]
    public class TilemapBlocker : TileTerrainComponent, IChunkJobScheduler
    {
        [SerializeField] private Tilemap[] tilemaps;

        private Dictionary<ChunkData, NativeList<GridModification>> modifierCache;

        public bool IsBlocking => true;

        private void OnEnable()
        {
            modifierCache = new Dictionary<ChunkData, NativeList<GridModification>>();
            TileTerrain.OnChunkInstantiated += OnChunkInitialized;
            TileTerrain.OnChunkDestroyed += OnChunkDestroyed;
        }

        private void OnChunkInitialized(int2 index, ChunkData chunkData)
        {
            chunkData.dependencies.Add(this);
        }

        private void OnChunkDestroyed(int2 index, ChunkData chunkData)
        {
            DisposeModifierList(chunkData);
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInstantiated -= OnChunkInitialized;
            TileTerrain.OnChunkDestroyed -= OnChunkDestroyed;
        }

        private NativeList<GridModification> GetModifierList(ChunkData chunkData)
        {
            if (!modifierCache.TryGetValue(chunkData, out NativeList<GridModification> result))
            {
                result = new NativeList<GridModification>(Allocator.Persistent);
                modifierCache[chunkData] = result;
            }
            return result;
        }

        private void DisposeModifierList(ChunkData chunkData)
        {
            if (modifierCache.TryGetValue(chunkData, out NativeList<GridModification> list))
            {
                list.Dispose();
                modifierCache.Remove(chunkData);
            }
        }

        public JobHandle ScheduleChunkJob(TileTerrain grid, ChunkData chunkData, JobHandle dependency)
        {
            NativeList<GridModification> modifiers = CreateGridModifications(chunkData);

            int voxelCount = chunkData.fillTypes.Length;
            ModifyFillTypeJob modifyFillJob = new ModifyFillTypeJob()
            {
                resolution = chunkData.Resolution,
                size = grid.TileSize,
                modifiers = modifiers,
                fillTypes = chunkData.fillTypes,
            };
            dependency = modifyFillJob.Schedule(voxelCount, 64, dependency);

            ModifyOffsetsJob modifyOffsetsJob = new ModifyOffsetsJob()
            {
                resolution = chunkData.Resolution,
                size = grid.TileSize,
                modifiers = modifiers,
                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
                normalsX = chunkData.normalsX,
                normalsY = chunkData.normalsY,
            };
            dependency = modifyOffsetsJob.Schedule(voxelCount, 64, dependency);

            return dependency;
        }

        private NativeList<GridModification> CreateGridModifications(ChunkData chunkData)
        {
            NativeList<GridModification> modifiers = GetModifierList(chunkData);
            modifiers.Clear();

            Rect bounds = chunkData.GetBounds();
            BoundsInt boundsInt = GetBoundsInt(bounds);

            for (int i = 0; i < tilemaps.Length; i++)
            {
                Tilemap tilemap = tilemaps[i];
                if (!bounds.Overlaps(GetBounds(tilemap.cellBounds)))
                    continue;

                BoundsInt overlap = boundsInt.GetOverlapArea(tilemap.cellBounds);
                foreach (Vector3Int position in overlap.allPositionsWithin)
                {
                    if (tilemap.HasTile(position))
                        modifiers.Add(CreateModifier(chunkData, position));
                }
            }
            return modifiers;
        }

        private GridModification CreateModifier(ChunkData chunkData, Vector3Int position)
        {
            return new GridModification()
            {
                ModifierShape = ModifierShape.Square,
                modifierType = ModifierType.Always,
                setFilltype = FillType.None,
                position = new float2(position.x, position.y) - chunkData.Origin,
                size = 1f,
            };
        }

        public void OnJobCompleted(ChunkData chunkData)
        {
            chunkData.dependencies.Remove(this);
            DisposeModifierList(chunkData);
        }

        private BoundsInt GetBoundsInt(Rect rect)
        {
            return new BoundsInt(
                new Vector3Int((int)rect.min.x, (int)rect.min.y, 0), 
                new Vector3Int((int)rect.size.x, (int)rect.size.y, 1));
        }

        private Rect GetBounds(BoundsInt boundsInt)
        {
            return new Rect(boundsInt.min.x, boundsInt.min.y, boundsInt.size.x, boundsInt.size.y);
        }
    }
}
