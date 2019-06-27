using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[ExecuteInEditMode]
public class ChunkRenderer : MonoBehaviour, IChunkJobDependency
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    private Mesh sharedMesh;

    private NativeMultiHashMap<int, Polygon> polygons;
    private NativeList<Vector3> vertices;
    private NativeList<int> triangleIndices;
    private NativeList<int> triangleLengths;

    private VoxelGrid currentGrid;
    private JobHandle? currentJobHandle;

    public static ChunkRenderer CreateNewInstance()
    {
        GameObject gameObject = new GameObject("Chunk Renderer");
        gameObject.hideFlags = HideFlags.DontSave;
        return gameObject.AddComponent<ChunkRenderer>();
    }

    private void Awake()
    {
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
    }

    private void OnEnable()
    {
        sharedMesh = new Mesh();
        meshFilter.sharedMesh = sharedMesh;

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

        DestroyImmediate(sharedMesh);
    }

    private void ClearJobData()
    {
        polygons.Clear();
        vertices.Clear();
        triangleIndices.Clear();
        triangleLengths.Clear();
    }

    public JobHandle ScheduleChunkJob(VoxelGrid grid, ChunkData chunkData, JobHandle dependency)
    {
        currentGrid = grid;

        ClearJobData();
        GenerateVoxelPolygonsJob generateVoxelPolygonsJob = new GenerateVoxelPolygonsJob()
        {
            resolution = currentGrid.ChunkResolution,
            size = currentGrid.VoxelSize,
            generateForFillTypes = currentGrid.SupportedFillTypes,
            fillTypes = chunkData.fillTypes,
            offsets = chunkData.offsets,
            polygons = polygons.ToConcurrent(),
        };
        currentJobHandle = generateVoxelPolygonsJob.Schedule(currentGrid.VoxelsPerChunk, 64, dependency);

        GenerateMeshDataJob generateMeshDataJob = new GenerateMeshDataJob()
        {
            polygons = polygons,
            vertices = vertices,
            generateForFillTypes = currentGrid.SupportedFillTypes,
            triangleIndices = triangleIndices,
            triangleLengths = triangleLengths,
        };
        currentJobHandle = generateMeshDataJob.Schedule(currentJobHandle.Value);
        return currentJobHandle.Value;
    }

    public void OnJobCompleted()
    {
        currentJobHandle = null;
        ApplyDataToMesh();
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
