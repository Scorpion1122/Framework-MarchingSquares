using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ChunkRenderer : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    private Mesh sharedMesh;

    private NativeMultiHashMap<int, Polygon> polygons;
    private NativeList<Vector3> vertices;
    private NativeList<int> triangleIndices;
    private NativeList<int> triangleLengths;

    private bool isChunkDataDirty;
    private VoxelGrid currentGrid;
    private ChunkData currentChunkData;
    private JobHandle? currentJobHandle;

    private void Awake()
    {
        sharedMesh = new Mesh();
        meshFilter.sharedMesh = sharedMesh;
    }

    private void OnEnable()
    {
        polygons = new NativeMultiHashMap<int, Polygon>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        vertices = new NativeList<Vector3>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        triangleIndices = new NativeList<int>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        triangleLengths = new NativeList<int>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
    }

    private void OnDisable()
    {
        if (currentJobHandle != null)
            currentJobHandle.Value.Complete();

        polygons.Dispose();
        vertices.Dispose();
        triangleIndices.Dispose();
        triangleLengths.Dispose();
    }

    public void OnChunkChanged(VoxelGrid grid, ChunkData chunkData)
    {
        isChunkDataDirty = true;
        currentChunkData = chunkData;
        currentGrid = grid;

        if (currentJobHandle == null)
            StartGenerateMeshJob();
    }

    private void StartGenerateMeshJob()
    {
        GenerateVoxelPolygonsJob generateVoxelPolygonsJob = new GenerateVoxelPolygonsJob()
        {
            resolution = currentGrid.Resolution,
            size = currentGrid.Size,
            generateForFillTypes = currentGrid.SupportedFillTypes,
            fillTypes = currentChunkData.fillTypes,
            offsets = currentChunkData.offsets,
            polygons = polygons.ToConcurrent(),
        };
        currentJobHandle = generateVoxelPolygonsJob.Schedule(currentGrid.VoxelsPerChunk, 64);

        GenerateMeshDataJob generateMeshDataJob = new GenerateMeshDataJob()
        {
            polygons = polygons,
            vertices = vertices,
            generateForFillTypes = currentGrid.SupportedFillTypes,
            triangleIndices = triangleIndices,
            triangleLengths = triangleLengths,
        };
        currentJobHandle = generateMeshDataJob.Schedule(currentJobHandle.Value);
    }

    private void LateUpdate()
    {
        if (currentJobHandle == null)
            return;

        if (!currentJobHandle.Value.IsCompleted)
            return;

        ApplyDataToMesh();
        currentJobHandle = null;

        if (isChunkDataDirty)
            StartGenerateMeshJob();
    }

    private void ApplyDataToMesh()
    {
        sharedMesh.Clear();

        int subMeshCount = GetSubMeshCount();
        Material[] materials = new Material[subMeshCount];
        sharedMesh.subMeshCount = subMeshCount;
        sharedMesh.vertices = vertices.ToArray();

        int offset = 0;
        int currentSubMesh = 0;
        for (int i = 0; i < triangleLengths.Length; i++)
        {
            int length = triangleLengths[i];
            if (length != 0)
            {
                int[] triangles = triangleIndices.ToArray(offset, length);
                meshFilter.sharedMesh.SetTriangles(triangles, currentSubMesh);
                materials[currentSubMesh] = currentGrid.MaterialTemplate.GetMaterial((FillType)(i + 1));
                currentSubMesh++;
            }
            offset += length;
        }
        meshRenderer.sharedMaterials = materials;
    }

    private int GetSubMeshCount()
    {
        int result = 0;
        for (int i = 0; i < triangleLengths.Length; i++)
        {
            if (triangleLengths[i] != 0)
                result++;
        }

        return result;
    }
}
