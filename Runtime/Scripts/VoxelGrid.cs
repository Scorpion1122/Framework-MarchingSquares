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

    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MaterialTemplate materialTemplate;

    public float Size => size;
    public int Resolution => resolution;
    public int VoxelsPerChunk => resolution * resolution;
    public MaterialTemplate MaterialTemplate => materialTemplate;
    public NativeArray<FillType> SupportedFillTypes => generateForFillTypes;

    private ChunkData chunkData;
    private NativeList<GridModification> modifiers;
    private NativeArray<FillType> generateForFillTypes;

    private void OnEnable()
    {
        meshFilter.sharedMesh = new Mesh();

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

        DestroyImmediate(meshFilter.sharedMesh);
    }

    public void ModifyGrid(GridModification modification)
    {
        modifiers.Add(modification);
        ScheduleModifyChunkJob(chunkData, modifiers);
        modifiers.Clear();
    }

    private void ScheduleModifyChunkJob(ChunkData chunkData, NativeList<GridModification> modifiers)
    {
        chunkData.ClearTempData();

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

        //Create polygons for all fill types
        GenerateVoxelPolygonsJob generateVoxelPolygonsJob = new GenerateVoxelPolygonsJob()
        {
            resolution = resolution,
            size = size,
            generateForFillTypes = generateForFillTypes,
            fillTypes = chunkData.fillTypes,
            offsets = chunkData.offsets,
            polygons = chunkData.polygons.ToConcurrent(),
        };
        jobHandle = generateVoxelPolygonsJob.Schedule(voxelCount, 64, jobHandle);


        GenerateMeshDataJob generateMeshDataJob = new GenerateMeshDataJob()
        {
            polygons = chunkData.polygons,
            vertices = chunkData.vertices,
            generateForFillTypes = generateForFillTypes,
            triangleIndices = chunkData.triangleIndices,
            triangleLengths = chunkData.triangleLengths,
        };
        jobHandle = generateMeshDataJob.Schedule(jobHandle);
        jobHandle.Complete();

        ColliderGenerationJob colliderGenerationJob = new ColliderGenerationJob()
        {
            resolution = resolution,
            size = size,
            fillTypes = chunkData.fillTypes,
            offsets = chunkData.offsets,

            generateForFillTypes = generateForFillTypes,

            processed = new NativeList<int>(1000, Allocator.TempJob),

            vertices = chunkData.colliderVertices,
            lengths = chunkData.colliderLengths,
            fillType = chunkData.colliderTypes,
        };
//        Profiler.BeginSample("Create Colliders");
//        colliderGenerationJob.Execute();
//        Profiler.EndSample();
        jobHandle = colliderGenerationJob.Schedule(jobHandle);
        jobHandle.Complete();

        colliderGenerationJob.processed.Dispose();

        //Temp immediate
        meshFilter.sharedMesh.Clear();

        int subMeshCount = GetSubMeshCount(chunkData);
        Material[] materials = new Material[subMeshCount];
        meshFilter.sharedMesh.subMeshCount = subMeshCount;
        meshFilter.sharedMesh.vertices = chunkData.vertices.ToArray();

        int offset = 0;
        int currentSubMesh = 0;
        for (int i = 0; i < chunkData.triangleLengths.Length; i++)
        {
            int length = chunkData.triangleLengths[i];
            if (length != 0)
            {
                int[] triangles = chunkData.triangleIndices.ToArray(offset, length);
                meshFilter.sharedMesh.SetTriangles(triangles, currentSubMesh);
                materials[currentSubMesh] = materialTemplate.GetMaterial((FillType)(i + 1));
                currentSubMesh++;
            }
            offset += length;
        }
        meshRenderer.sharedMaterials = materials;

    }

    private int GetSubMeshCount(ChunkData chunkData)
    {
        int result = 0;
        for (int i = 0; i < chunkData.triangleLengths.Length; i++)
        {
            if (chunkData.triangleLengths[i] != 0)
                result++;
        }

        return result;
    }

    private void OnDrawGizmos()
    {
        if (chunkData == null)
            return;

        //VoxelGizmos.DrawVoxels(transform, chunkData, resolution, size);
        //VoxelGizmos.DrawColliders(transform, chunkData);
    }
}
