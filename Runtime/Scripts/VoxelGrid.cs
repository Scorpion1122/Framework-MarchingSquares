using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace Thijs.Framework.MarchingSquares
{
    [ExecuteInEditMode]
    public class VoxelGrid : MonoBehaviour
    {
        [SerializeField] private int gridResolution = 2;

        [Header("Chunk Configuration")] [FormerlySerializedAs("resolution")] [SerializeField]
        private int chunkResolution = 128;

        [FormerlySerializedAs("size")] [SerializeField]
        private float voxelSize = 1f;

        [SerializeField] private MaterialTemplate materialTemplate = null;
        [SerializeField] private WorldGeneration worldGenerationTest = null;

        [Header("Debug")] [SerializeField] private bool drawGizmos = false;

        public float VoxelSize => voxelSize;
        public int ChunkResolution => chunkResolution + 1;
        public int VoxelsPerChunk => ChunkResolution * ChunkResolution;
        public MaterialTemplate MaterialTemplate => materialTemplate;
        public NativeArray<FillType> SupportedFillTypes => supportedFillTypes;

        private float chunkSize;
        private ChunkData[] chunks;
        private ChunkRenderer[] renderers;
        private ChunkCollider[] colliders;

        private NativeArray<FillType> supportedFillTypes;
        private List<ChunkData> activeJobHandles;
        private HashSet<int> dirtyChunks;

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            chunkSize = voxelSize * chunkResolution;

            dirtyChunks = new HashSet<int>();
            activeJobHandles = new List<ChunkData>();

            chunks = new ChunkData[gridResolution * gridResolution];
            renderers = new ChunkRenderer[chunks.Length];
            colliders = new ChunkCollider[chunks.Length];

            for (int i = 0; i < chunks.Length; i++)
            {
                float2 origin = ChunkUtility.GetChunkOrigin(i, gridResolution, chunkSize);
                chunks[i] = new ChunkData(origin, chunkSize, chunkResolution + 1);

                //Generate Data
                if (worldGenerationTest != null)
                {
                    worldGenerationTest.GenerateChunkData(this, chunks[i]);
                    dirtyChunks.Add(i);
                }

                renderers[i] = ChunkRenderer.CreateNewInstance(transform);
                renderers[i].transform.position = transform.TransformPoint(origin.x, origin.y, 0f);

                colliders[i] = ChunkCollider.CreateNewInstance(transform);
                colliders[i].transform.position = transform.TransformPoint(origin.x, origin.y, 0f);
            }

            FillType[] allFillTypes = (FillType[]) Enum.GetValues(typeof(FillType));
            supportedFillTypes = new NativeArray<FillType>(allFillTypes.Length - 1, Allocator.Persistent);
            for (int i = 1; i < allFillTypes.Length; i++)
            {
                supportedFillTypes[i - 1] = allFillTypes[i];
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].Dispose();
                DestroyImmediate(renderers[i].gameObject);
                DestroyImmediate(colliders[i].gameObject);
            }

            chunks = null;
            renderers = null;
            colliders = null;

            supportedFillTypes.Dispose();
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
                if (chunkBounds.Intersects(modificationBounds))
                    AddModifierToChunk(i, modification);
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
            Profiler.BeginSample("Voxel Grid - Late Update");
            ScheduleModifyChunkJobs();
            CompleteModifyChunkJobs();
            Profiler.EndSample();
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

                ScheduleModifyChunkJob(chunkIndex, chunkData);
                activeJobHandles.Add(chunkData);
            }

            dirtyChunks.Clear();

            JobHandle.ScheduleBatchedJobs();
        }

        private void CompleteModifyChunkJobs()
        {
            foreach (ChunkData chunkData in activeJobHandles)
            {
                CompleteChunkJobs(chunkData);
            }
            activeJobHandles.Clear();
        }

        private void ScheduleModifyChunkJob(int chunkIndex, ChunkData chunkData)
        {
            int voxelCount = chunkData.fillTypes.Length;
            ModifyFillTypeJob modifyFillJob = new ModifyFillTypeJob()
            {
                resolution = chunkData.resolution,
                size = voxelSize,
                modifiers = chunkData.modifiers,
                fillTypes = chunkData.fillTypes,
            };
            JobHandle jobHandle = modifyFillJob.Schedule(voxelCount, 64);

            ModifyOffsetsJob modifyOffsetsJob = new ModifyOffsetsJob()
            {

                resolution = chunkData.resolution,
                size = voxelSize,
                modifiers = chunkData.modifiers,
                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
            };
            jobHandle = modifyOffsetsJob.Schedule(voxelCount, 64, jobHandle);

            GetChunkDependencies(chunkIndex, chunkData);
            for (int i = 0; i < chunkData.dependencies.Count; i++)
            {
                JobHandle dependencyHandle = chunkData.dependencies[i].ScheduleChunkJob(this, chunkData, jobHandle);
                jobHandle = JobHandle.CombineDependencies(dependencyHandle, jobHandle);
            }

            chunkData.jobHandle = jobHandle;
        }

        private void CompleteChunkJobs(ChunkData chunkData)
        {
            if (chunkData.jobHandle == null)
                return;

            chunkData.jobHandle.Value.Complete();
            for (int i = 0; i < chunkData.dependencies.Count; i++)
            {
                chunkData.dependencies[i].OnJobCompleted();
            }

            chunkData.dependencies.Clear();
            chunkData.modifiers.Clear();
            chunkData.jobHandle = null;
        }

        private void GetChunkDependencies(int chunkIndex, ChunkData chunkData)
        {
            chunkData.dependencies.Add(renderers[chunkIndex]);
            chunkData.dependencies.Add(colliders[chunkIndex]);
        }

        private void OnDrawGizmos()
        {
            if (chunks == null || !drawGizmos)
                return;

            for (int i = chunks.Length - 1; i >= 0; i--)
            {
                ChunkData chunkData = chunks[i];
                if (chunkData == null)
                    continue;

                VoxelGizmos.DrawVoxels(transform, chunkData, voxelSize);

                float2 center = chunkData.origin + chunkSize * 0.5f;
                Vector3 worldCenter = transform.TransformPoint(new Vector3(center.x, center.y, 0));
                Gizmos.DrawWireCube(worldCenter, new Vector3(voxelSize, voxelSize, 0) * chunkResolution);
            }

            //VoxelGizmos.DrawColliders(transform, chunkData);
        }
    }
}
