using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class ChunkCollider : MonoBehaviour, IChunkJobDependency
{
    private NativeList<float2> vertices;
    private NativeList<int> lengths;
    private NativeList<FillType> types;
    private NativeList<int> processedCache;

    private VoxelGrid currentGrid;
    private JobHandle? currentJobHandle;

    private List<EdgeCollider2D> colliders;

    public static ChunkCollider CreateNewInstance()
    {
        GameObject gameObject = new GameObject("Chunk Collider");
        gameObject.hideFlags = HideFlags.DontSave;
        return gameObject.AddComponent<ChunkCollider>();
    }

    private void OnEnable()
    {
        vertices = new NativeList<float2>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        lengths = new NativeList<int>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        types = new NativeList<FillType>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        processedCache = new NativeList<int>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        colliders = new List<EdgeCollider2D>();
    }

    private void OnDisable()
    {
        if (currentJobHandle != null)
            currentJobHandle.Value.Complete();

        vertices.Dispose();
        lengths.Dispose();
        types.Dispose();
        processedCache.Dispose();

        EnsureColliderCapacity(0);
        colliders = null;
    }

    private void ClearJobData()
    {
        vertices.Clear();
        lengths.Clear();
        types.Clear();
        processedCache.Clear();
    }

    public JobHandle ScheduleChunkJob(VoxelGrid grid, ChunkData chunkData, JobHandle dependency)
    {
        currentGrid = grid;
        ClearJobData();

        ColliderGenerationJob colliderGenerationJob = new ColliderGenerationJob()
        {
            //Input
            resolution = currentGrid.Resolution,
            size = currentGrid.Size,
            fillTypes = chunkData.fillTypes,
            offsets = chunkData.offsets,
            generateForFillTypes = currentGrid.SupportedFillTypes,

            //Output
            vertices = vertices,
            lengths = lengths,
            fillType = types,
            processed = processedCache,
        };
        currentJobHandle = colliderGenerationJob.Schedule(dependency);

        return currentJobHandle.Value;
    }

    public void OnJobCompleted()
    {
        EnsureColliderCapacity(lengths.Length);
    }

    private void EnsureColliderCapacity(int amount)
    {
        int overflow = colliders.Count - amount;
        for (int i = 0; i < overflow; i++)
        {
            DestroyImmediate(colliders[colliders.Count - 1]);
            colliders.RemoveAt(colliders.Count - 1);
        }

        int missing = amount - colliders.Count;
        for (int i = 0; i < missing; i++)
        {
            colliders.Add(gameObject.AddComponent<EdgeCollider2D>());
        }
    }
}
