using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[ExecuteInEditMode]
public class VoxelGrid : MonoBehaviour
{
    [SerializeField] private int resolution = 128;
    [SerializeField] private float size = 1f;

    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MaterialTemplate materialTemplate;

    private ChunkData chunkData;
    private NativeList<GridModification> modifiers;
    private NativeArray<FillType> fillTypes;

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
        fillTypes = new NativeArray<FillType>(allFillTypes.Length - 1, Allocator.Persistent);
        for (int i = 1; i < allFillTypes.Length; i++)
        {
            fillTypes[i - 1] = allFillTypes[i];
        }
    }

    private void OnDisable()
    {
        chunkData.Dispose();
        chunkData = null;

        modifiers.Dispose();
        fillTypes.Dispose();

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
            generateForFillTypes = fillTypes,
            fillTypes = chunkData.fillTypes,
            offsets = chunkData.offsets,
            polygons = chunkData.polygons.ToConcurrent(),
        };
        jobHandle = generateVoxelPolygonsJob.Schedule(voxelCount, 64, jobHandle);


        GenerateMeshDataJob generateMeshDataJob = new GenerateMeshDataJob()
        {
            polygons = chunkData.polygons,
            vertices = chunkData.vertices,
            generateForFillTypes = fillTypes,
            triangleIndices = chunkData.triangleIndices,
            triangleLengths = chunkData.triangleLengths,
        };
        jobHandle = generateMeshDataJob.Schedule(jobHandle);
        jobHandle.Complete();


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

        chunkData.ClearTempData();
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

        VoxelGizmos.DrawVoxels(transform, chunkData, resolution, size);
    }
}
