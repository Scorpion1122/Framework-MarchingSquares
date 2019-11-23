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
        
        //New
        private NativeList<float2> jobVertices;
        private List<Vector3> vertexCache;
        
        private NativeList<int> triangleIndices;
        private NativeList<int> triangleLengths;

        private VoxelGrid currentGrid;
        private JobHandle? currentJobHandle;

        public static ChunkRenderer CreateNewInstance(Transform parent)
        {
            GameObject gameObject = new GameObject("Chunk Renderer");
            gameObject.hideFlags = HideFlags.DontSave;
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<ChunkRenderer>();
        }

        private void Awake()
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter = gameObject.AddComponent<MeshFilter>();
            vertexCache = new List<Vector3>();
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

            DestroyImmediate(sharedMesh);
        }

        private void ClearJobData()
        {
            jobVertices.Clear();
            triangleIndices.Clear();
            triangleLengths.Clear();
        }

        public JobHandle ScheduleChunkJob(VoxelGrid grid, ChunkData chunkData, JobHandle dependency)
        {
            currentGrid = grid;

            ClearJobData();
            SinglePassGenerateMeshDataJob singlePassMeshGenJob = new SinglePassGenerateMeshDataJob()
            {
                resolution = currentGrid.ChunkResolution,
                size = currentGrid.VoxelSize,
                generateForFillTypes = currentGrid.SupportedFillTypes,
                
                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
                
                vertices = jobVertices,
                triangleIndices = triangleIndices,
                triangleLengths = triangleLengths,
            };
            currentJobHandle = singlePassMeshGenJob.Schedule(dependency);
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

            WriteJobVerticesToVertexCache();
            sharedMesh.SetVertices(vertexCache);
            
            int offset = 0;
            int currentSubMesh = 0;
            for (int i = 0; i < triangleLengths.Length; i++)
            {
                int length = triangleLengths[i];
                if (length != 0)
                {
                    int[] triangles = triangleIndices.ToArray(offset, length);
                    meshFilter.sharedMesh.SetTriangles(triangles, currentSubMesh);
                    materials[currentSubMesh] = currentGrid.MaterialTemplate.GetMaterial((FillType) (i + 1));
                    currentSubMesh++;
                }

                offset += length;
            }

            meshRenderer.sharedMaterials = materials;
        }

        private void WriteJobVerticesToVertexCache()
        {
            vertexCache.Clear();
            for (int i = 0; i < jobVertices.Length; i++)
            {
                float2 jobVertex = jobVertices[i];
                vertexCache.Add(new Vector3(jobVertex.x, jobVertex.y));
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
