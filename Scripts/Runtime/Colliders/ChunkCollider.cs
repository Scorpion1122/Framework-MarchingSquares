using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [ExecuteInEditMode]
    public class ChunkCollider : MonoBehaviour, IChunkJobDependency
    {
        private NativeList<float2> vertices;
        private NativeList<int> lengths;
        private NativeList<FillType> types;
        private NativeList<int> processedCache;

        private TileTerrain currentGrid;
        private JobHandle? currentJobHandle;

        private Dictionary<int, ColliderPool> colliderPools = new Dictionary<int, ColliderPool>();

        public bool IsBlocking => false;

        private void OnEnable()
        {
            vertices = new NativeList<float2>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
            lengths = new NativeList<int>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
            types = new NativeList<FillType>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
            processedCache = new NativeList<int>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        }

        private void OnDisable()
        {
            if (currentJobHandle != null)
                currentJobHandle.Value.Complete();

            if (vertices.IsCreated)
            {
                vertices.Dispose();
                lengths.Dispose();
                types.Dispose();
                processedCache.Dispose();
            }

            foreach (var pool in colliderPools)
            {
                pool.Value.Dispose();
            }
            colliderPools.Clear();
        }

        private void ClearJobData()
        {
            vertices.Clear();
            lengths.Clear();
            types.Clear();
            processedCache.Clear();
        }

        public JobHandle ScheduleChunkJob(TileTerrain grid, ChunkData chunkData, JobHandle dependency)
        {
            currentGrid = grid;
            ClearJobData();

            ColliderGenerationJob colliderGenerationJob = new ColliderGenerationJob()
            {
                //Input
                resolution = currentGrid.ChunkResolution,
                size = currentGrid.TileSize,
                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
                supportedFillTypes = currentGrid.SupportedFillTypes,

                //Output
                vertices = vertices,
                lengths = lengths,
                colliderFillTypes = types,
                processed = processedCache,
            };
            currentJobHandle = colliderGenerationJob.Schedule(dependency);

            return currentJobHandle.Value;
        }

        public void OnJobCompleted(ChunkData chunkData)
        {
            ResetColliderPools();
            int offset = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                int length = lengths[i];
                FillType fillType = types[i];
                int layer = GetLayerForFillType(fillType);
                PhysicsMaterial2D material = GetMaterialForFillType(fillType);

                Vector2[] points = new Vector2[length];
                for (int j = 0; j < length; j++)
                {
                    float2 vertex = vertices[j + offset];
                    points[j] = new Vector2(vertex.x, vertex.y);
                }
                
                ColliderPool pool = GetColliderPool(layer);
                pool.AddEdge(material, points);

                offset += length;
            }
            ClearUnusedColliders();
        }

        private void ResetColliderPools()
        {
            foreach (var colliderPool in colliderPools)
            {
                colliderPool.Value.ResetUsage();
            }
        }
        
        private void ClearUnusedColliders()
        {
            foreach (var colliderPool in colliderPools)
            {
                colliderPool.Value.ClearUnused();
            }
        }

        private ColliderPool GetColliderPool(int layer)
        {
            ColliderPool pool;
            if (!colliderPools.TryGetValue(layer, out pool))
            {
                pool = new ColliderPool(transform, layer);
                colliderPools.Add(layer, pool);
            }
            return pool;
        }

        private int GetLayerForFillType(FillType fillType)
        {
            if (currentGrid.TileTemplate != null)
                return currentGrid.TileTemplate.GetLayer(fillType);
            return Layers.DEFAULT;
        }

        private PhysicsMaterial2D GetMaterialForFillType(FillType fillType)
        {
            if (currentGrid.TileTemplate != null)
                return currentGrid.TileTemplate.GetPhysicsMaterial(fillType);
            return null;
        }
    }
}
