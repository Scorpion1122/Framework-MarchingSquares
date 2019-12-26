using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [ExecuteInEditMode]
    public class ChunkRenderer : MonoBehaviour, IChunkJobDependency
    {
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshFilter meshFilter;
        private Mesh sharedMesh;
        
        private NativeList<float2> jobVertices;
        private List<Vector3> vertices;
        private List<int> triangles;
        
        private NativeList<int> triangleIndices;
        private NativeList<int> triangleLengths;
        private VertexCache vertexCache;

        private TileTerrain currentGrid;
        private JobHandle? currentJobHandle;

        public bool IsBlocking => false;

        private void Awake()
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter = gameObject.AddComponent<MeshFilter>();
            vertices = new List<Vector3>(VoxelUtility.NATIVE_CACHE_SIZE);
            triangles = new List<int>(VoxelUtility.NATIVE_CACHE_SIZE);
        }

        private void OnEnable()
        {
            sharedMesh = new Mesh();
            meshFilter.sharedMesh = sharedMesh;

            jobVertices = new NativeList<float2>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
            triangleIndices = new NativeList<int>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
            triangleLengths = new NativeList<int>(VoxelUtility.NATIVE_CACHE_SIZE, Allocator.Persistent);
        }

        private void OnDisable()
        {
            if (currentJobHandle != null)
                currentJobHandle.Value.Complete();

            if (jobVertices.IsCreated)
            {
                jobVertices.Dispose();
                triangleIndices.Dispose();
                triangleLengths.Dispose();
            }
            
            if (vertexCache.IsCreated)
                vertexCache.Dispose();

            DestroyImmediate(sharedMesh);
        }

        private void ClearJobData()
        {
            jobVertices.Clear();
            triangleIndices.Clear();
            triangleLengths.Clear();
        }

        private void InitializeVertexCache(TileTerrain grid)
        {
            if (!vertexCache.IsCreated)
            {
                vertexCache = new VertexCache(grid.ChunkResolution);
            }
            else if (vertexCache.resolution != grid.ChunkResolution)
            {
                vertexCache.Dispose();
                vertexCache = new VertexCache(grid.ChunkResolution);
            }
        }

        public JobHandle ScheduleChunkJob(TileTerrain grid, ChunkData chunkData, JobHandle dependency)
        {
            currentGrid = grid;
            InitializeVertexCache(currentGrid);

            ClearJobData();
            SinglePassGenerateMeshDataJob singlePassMeshGenJob = new SinglePassGenerateMeshDataJob()
            {
                resolution = currentGrid.ChunkResolution,
                size = currentGrid.TileSize,
                generateForFillTypes = currentGrid.SupportedFillTypes,
                
                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
                normalsX = chunkData.normalsX,
                normalsY = chunkData.normalsY,

                sharpnessLimit = math.cos(math.radians(grid.SharpnessLimit)),
                
                vertices = jobVertices,
                triangleIndices = triangleIndices,
                triangleLengths = triangleLengths,
                
                cache = vertexCache,
            };
            currentJobHandle = singlePassMeshGenJob.Schedule(dependency);
            return currentJobHandle.Value;
        }

        public void OnJobCompleted(ChunkData chunkData)
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

            WriteJobVerticesToVertexCache();
            sharedMesh.SetVertices(vertices);
            
            int offset = 0;
            int currentSubMesh = 0;
            for (int i = 0; i < triangleLengths.Length; i++)
            {
                int length = triangleLengths[i];
                if (length != 0)
                {
                    WriteJobTrianglesToTriangleCache(offset, length);
                    meshFilter.sharedMesh.SetTriangles(triangles, currentSubMesh);
                    
                    if (currentGrid.TileTemplate != null)
                        materials[currentSubMesh] = currentGrid.TileTemplate.GetMaterial((FillType) (i + 1));
                    currentSubMesh++;
                }

                offset += length;
            }

            meshRenderer.sharedMaterials = materials;
        }

        private void WriteJobVerticesToVertexCache()
        {
            vertices.Clear();
            for (int i = 0; i < jobVertices.Length; i++)
            {
                float2 jobVertex = jobVertices[i];
                vertices.Add(new Vector3(jobVertex.x, jobVertex.y));
            }
        }

        private void WriteJobTrianglesToTriangleCache(int offset, int length)
        {
            triangles.Clear();
            for (int i = offset; i < offset + length; i++)
            {
                triangles.Add(triangleIndices[i]);
            }
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
}
