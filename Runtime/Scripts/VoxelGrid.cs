using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class VoxelGrid : MonoBehaviour
{
    [SerializeField] private int gridResolution = 2;

    [Header("Chunk Configuration")]
    [FormerlySerializedAs("resolution")] [SerializeField] private int chunkResolution = 128;
    [FormerlySerializedAs("size")] [SerializeField] private float voxelSize = 1f;
    [SerializeField] private MaterialTemplate materialTemplate;
    [SerializeField] private WorldGeneration worldGenerationTest;

    public float VoxelSize => voxelSize;
    public int ChunkResolution => chunkResolution;
    public int VoxelsPerChunk => chunkResolution * chunkResolution;
    public MaterialTemplate MaterialTemplate => materialTemplate;
    public NativeArray<FillType> SupportedFillTypes => generateForFillTypes;

    private ChunkRenderer chunkRenderer;
    private ChunkCollider chunkCollider;

    private float chunkSize;
    private ChunkData[] chunks;

    private NativeArray<FillType> generateForFillTypes;
    private List<ChunkData> activeJobHandles;
    private HashSet<int> dirtyChunks;

    private void OnEnable()
    {
        chunkRenderer = ChunkRenderer.CreateNewInstance();
        chunkCollider = ChunkCollider.CreateNewInstance();

        Initialize();
    }

    private void Initialize()
    {
        chunkSize = voxelSize * chunkResolution;

        dirtyChunks = new HashSet<int>();
        activeJobHandles = new List<ChunkData>();

        chunks = new ChunkData[gridResolution * gridResolution];
        for (int i = 0; i < chunks.Length; i++)
        {
            float2 origin = ChunkUtility.GetChunkOrigin(i, gridResolution, chunkSize);
            chunks[i] = new ChunkData(origin, chunkSize, chunkResolution);
        }

        FillType[] allFillTypes = (FillType[])Enum.GetValues(typeof(FillType));
        generateForFillTypes = new NativeArray<FillType>(allFillTypes.Length - 1, Allocator.Persistent);
        for (int i = 1; i < allFillTypes.Length; i++)
        {
            generateForFillTypes[i - 1] = allFillTypes[i];
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Dispose();
        }
        chunks = null;

        generateForFillTypes.Dispose();

        DestroyImmediate(chunkRenderer.gameObject);
        DestroyImmediate(chunkCollider.gameObject);
    }

    public void ModifyGrid(GridModification modification)
    {
        Rect modificationBounds = modification.GetBounds();

        for (int i = 0; i < chunks.Length; i++)
        {
            ChunkData chunk = chunks[i];
            if (chunk == null)
                continue;

            Rect chunkBounds = chunk.GetBounds();
            Debug.Log(chunkBounds.Intersects(modificationBounds));
            if (chunkBounds.Intersects(modificationBounds))
                AddModifierToChunk(9, modification);
        }
    }

    private void AddModifierToChunk(int index, GridModification modification)
    {
        if (!ChunkUtility.IsChunkIndexValid(index, gridResolution))
            return;

        ChunkData chunkData = chunks[index];
        if (chunkData == null)
            return;

        modification.position = modification.position - chunkData.origin;
        chunkData.modifiers.Add(modification);

        if (!dirtyChunks.Contains(index))
            dirtyChunks.Add(index);
    }

    private void LateUpdate()
    {
        ScheduleModifyChunkJobs();
    }

    private void ScheduleModifyChunkJobs()
    {
        if (dirtyChunks.Count == 0)
            return;

        foreach (int chunkIndex in dirtyChunks)
        {
            ChunkData chunkData = chunks[chunkIndex];
            if (chunkData == null)
                continue;

            ScheduleModifyChunkJob(chunkData);
            activeJobHandles.Add(chunkData);
        }

        JobHandle.ScheduleBatchedJobs();

        foreach (ChunkData chunkData in activeJobHandles)
        {
            if (chunkData.jobHandle != null)
            {
                chunkData.jobHandle.Value.Complete();
                chunkData.modifiers.Clear();
            }
            chunkData.jobHandle = null;
        }
    }

    private void ScheduleModifyChunkJob(ChunkData chunkData)
    {
        int voxelCount = chunkData.fillTypes.Length;
        ModifyFillTypeJob modifyFillJob = new ModifyFillTypeJob()
        {
            resolution = chunkResolution,
            size = voxelSize,
            modifiers = chunkData.modifiers,
            fillTypes = chunkData.fillTypes,
        };
        JobHandle jobHandle = modifyFillJob.Schedule(voxelCount, 64);

        ModifyOffsetsJob modifyOffsetsJob = new ModifyOffsetsJob()
        {

            resolution = chunkResolution,
            size = voxelSize,
            modifiers = chunkData.modifiers,
            fillTypes = chunkData.fillTypes,
            offsets = chunkData.offsets,
        };
        jobHandle = modifyOffsetsJob.Schedule(voxelCount, 64, jobHandle);

        //Rendering
//        JobHandle meshHandle = chunkRenderer.ScheduleChunkJob(this, chunkData, jobHandle);
//        JobHandle colliderHandle = chunkCollider.ScheduleChunkJob(this, chunkData, jobHandle);
//
//        jobHandle = JobHandle.CombineDependencies(meshHandle, jobHandle);
//        jobHandle = JobHandle.CombineDependencies(colliderHandle, jobHandle);

        //jobHandle.Complete();

//        chunkRenderer.OnJobCompleted();
//        chunkCollider.OnJobCompleted();

        chunkData.jobHandle = jobHandle;
    }

    private void OnDrawGizmos()
    {
        if (chunks == null)
            return;

        Gizmos.DrawWireCube(
            transform.position + new Vector3(0.5f, 0.5f, 0) * chunkResolution * voxelSize,
            new Vector3(1 * voxelSize, 1 * voxelSize, 0) * chunkResolution);

        for (int i = 0; i < chunks.Length; i++)
        {
            ChunkData chunkData = chunks[i];
            if (chunkData != null)
                VoxelGizmos.DrawVoxels(transform, chunkData, chunkResolution, voxelSize);
        }

        //VoxelGizmos.DrawColliders(transform, chunkData);
    }
}
