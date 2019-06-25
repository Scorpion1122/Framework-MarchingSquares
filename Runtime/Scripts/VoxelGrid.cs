using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

[ExecuteInEditMode]
public class VoxelGrid : MonoBehaviour
{
    [SerializeField] private int resolution = 128;
    [SerializeField] private float size = 1f;
    [SerializeField] private MaterialTemplate materialTemplate;

    public float Size => size;
    public int Resolution => resolution;
    public int VoxelsPerChunk => resolution * resolution;
    public MaterialTemplate MaterialTemplate => materialTemplate;
    public NativeArray<FillType> SupportedFillTypes => generateForFillTypes;

    private ChunkData chunkData;
    private ChunkRenderer chunkRenderer;
    private ChunkCollider chunkCollider;

    private NativeList<GridModification> modifiers;
    private NativeArray<FillType> generateForFillTypes;

    private void OnEnable()
    {
        chunkRenderer = ChunkRenderer.CreateNewInstance();
        chunkCollider = ChunkCollider.CreateNewInstance();

        Initialize();
    }

    private void Initialize()
    {
        chunkData = new ChunkData(resolution);
        modifiers = new NativeList<GridModification>(Allocator.Persistent);

        FillType[] allFillTypes = (FillType[])Enum.GetValues(typeof(FillType));
        generateForFillTypes = new NativeArray<FillType>(allFillTypes.Length - 1, Allocator.Persistent);
        for (int i = 1; i < allFillTypes.Length; i++)
        {
            generateForFillTypes[i - 1] = allFillTypes[i];
        }
    }

    private void OnDisable()
    {
        chunkData.Dispose();
        chunkData = null;

        modifiers.Dispose();
        generateForFillTypes.Dispose();

        DestroyImmediate(chunkRenderer.gameObject);
        DestroyImmediate(chunkCollider.gameObject);
    }

    public void ModifyGrid(GridModification modification)
    {
        modifiers.Add(modification);
        ScheduleModifyChunkJob(chunkData, modifiers);
        modifiers.Clear();
    }

    private void ScheduleModifyChunkJob(ChunkData chunkData, NativeList<GridModification> modifiers)
    {
        int voxelCount = chunkData.fillTypes.Length;
        ModifyFillTypeJob modifyFillJob = new ModifyFillTypeJob()
        {
            resolution = resolution,
            size = size,
            modifiers = modifiers,
            fillTypes = chunkData.fillTypes,
        };
        JobHandle jobHandle = modifyFillJob.Schedule(voxelCount, 64);

        ModifyOffsetsJob modifyOffsetsJob = new ModifyOffsetsJob()
        {

            resolution = resolution,
            size = size,
            modifiers = modifiers,
            fillTypes = chunkData.fillTypes,
            offsets = chunkData.offsets,
        };
        jobHandle = modifyOffsetsJob.Schedule(voxelCount, 64, jobHandle);

        //Rendering
        JobHandle meshHandle = chunkRenderer.ScheduleChunkJob(this, chunkData, jobHandle);
        JobHandle colliderHandle = chunkCollider.ScheduleChunkJob(this, chunkData, jobHandle);

        jobHandle = JobHandle.CombineDependencies(meshHandle, jobHandle);
        jobHandle = JobHandle.CombineDependencies(colliderHandle, jobHandle);

        jobHandle.Complete();

        chunkRenderer.OnJobCompleted();
        chunkCollider.OnJobCompleted();
    }

//    private void LateUpdate()
//    {
//        if (chunkData.jobHandle != null)
//        {
//            chunkData.jobHandle.Value.Complete();
//            chunkData.jobHandle = null;
//        }
//    }

    private void OnDrawGizmos()
    {
        if (chunkData == null)
            return;

        VoxelGizmos.DrawVoxels(transform, chunkData, resolution, size);
        //VoxelGizmos.DrawColliders(transform, chunkData);
    }
}
