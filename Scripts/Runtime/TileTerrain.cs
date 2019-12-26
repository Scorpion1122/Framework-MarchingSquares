using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace Thijs.Framework.MarchingSquares
{
    [ExecuteInEditMode, DefaultExecutionOrder(-10)]
    public class TileTerrain : MonoBehaviour
    {
        public event Action<int2, ChunkData> OnChunkInitialized;
        
        [Header("Chunk Configuration")] 
        [SerializeField]
        private int chunkResolution = 64;
        [SerializeField]
        private float tileSize = 1f;
        [SerializeField]
        private float sharpnessLimit = 135;

        [FormerlySerializedAs("materialTemplate")] [SerializeField] private TileTemplate tileTemplate = null;

        [SerializeField] private WorldGeneration worldGeneration;

        [Header("Debug")] 
        [SerializeField] private bool drawGizmos = false;

        public float TileSize => tileSize;
        public int ChunkResolution => chunkResolution + 1;
        public int TilesPerChunk => ChunkResolution * ChunkResolution;
        public float ChunkSize => chunkSize;
        public float SharpnessLimit => sharpnessLimit;
        public TileTemplate TileTemplate => tileTemplate;
        public NativeArray<FillType> SupportedFillTypes => supportedFillTypes;

        private float chunkSize;
        private Dictionary<int2, ChunkData> chunks;

        private List<GridModification> scheduledModifications = new List<GridModification>();
        private NativeArray<FillType> supportedFillTypes;
        private List<ChunkData> activeJobHandles;
        private HashSet<int2> dirtyChunks;

        private Coroutine routine;
        
        private void OnEnable()
        {
            Initialize();
            routine = StartCoroutine(EndOfFrameEnumerator());
        }

        private void Initialize()
        {
            chunkSize = tileSize * chunkResolution;

            chunks = new Dictionary<int2, ChunkData>();
            dirtyChunks = new HashSet<int2>();
            activeJobHandles = new List<ChunkData>();

            FillType[] allFillTypes = (FillType[]) Enum.GetValues(typeof(FillType));
            supportedFillTypes = new NativeArray<FillType>(allFillTypes.Length - 1, Allocator.Persistent);
            for (int i = 1; i < allFillTypes.Length; i++)
            {
                supportedFillTypes[i - 1] = allFillTypes[i];
            }
        }

        private void OnDisable()
        {
            StopCoroutine(routine);
            foreach (var chunkData in chunks)
            {
                DisposeOfChunk(chunkData.Value);
            }
            dirtyChunks.Clear();
            chunks = null;
            supportedFillTypes.Dispose();
        }
        
        #region Loading
        public void LoadChunk(int2 chunkIndex)
        {
            InitializeChunk(chunkIndex);
        }

        private void InitializeChunk(int2 chunkIndex)
        {
            float2 origin = ChunkUtility.GetChunkOrigin(chunkIndex, chunkSize);
            ChunkData chunkData = new ChunkData(origin, chunkSize, chunkResolution + 1);

            if (worldGeneration != null)
                worldGeneration.GenerateChunkData(this, chunkData);

            chunks.Add(chunkIndex, chunkData);
            dirtyChunks.Add(chunkIndex);
            
            OnChunkInitialized?.Invoke(chunkIndex, chunkData);
        }

        public void UnloadChunk(int2 chunkIndex)
        {
            if (!IsChunkActive((chunkIndex)))
                return;
            DisposeOfChunk(chunks[chunkIndex]);
            chunks.Remove(chunkIndex);
        }

        private void DisposeOfChunk(ChunkData chunkData)
        {
            foreach (var dependency in chunkData.dependencies)
            {
                if (dependency is Component component)
                {
                    DestroyImmediate(component.gameObject);
                }
            }
            chunkData.Dispose();
        }

        public bool IsChunkActive(int2 chunkIndex)
        {
            return chunks.ContainsKey(chunkIndex);
        }
        #endregion

        #region Job Scheduling
        // Start jobs before rendering
        private void LateUpdate()
        {
            Profiler.BeginSample("Voxel Grid - Late Update");
            CompleteChunkJobs();
            AddScheduledModificationsToChunks();
            ScheduleChunkJobs();
            
            if (!Application.isPlaying)
                CompleteChunkJobs();
            Profiler.EndSample();
        }

        // Wait for after rendering
        private IEnumerator EndOfFrameEnumerator()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                CompleteChunkJobs();
            }
        }

        private void ScheduleChunkJobs()
        {
            if (dirtyChunks.Count == 0)
                return;

            foreach (int2 chunkIndex in dirtyChunks)
            {
                ChunkData chunkData = chunks[chunkIndex];
                if (chunkData == null)
                    continue;

                ScheduleChunkJob(chunkData);
                activeJobHandles.Add(chunkData);
            }

            dirtyChunks.Clear();

            JobHandle.ScheduleBatchedJobs();
        }

        private void CompleteChunkJobs()
        {
            foreach (ChunkData chunkData in activeJobHandles)
            {
                CompleteChunkJobs(chunkData);
            }
            activeJobHandles.Clear();
        }

        private void ScheduleChunkJob(ChunkData chunkData)
        {
            int voxelCount = chunkData.fillTypes.Length;
            ModifyFillTypeJob modifyFillJob = new ModifyFillTypeJob()
            {
                resolution = chunkData.Resolution,
                size = tileSize,
                modifiers = chunkData.modifiers,
                fillTypes = chunkData.fillTypes,
            };
            JobHandle jobHandle = modifyFillJob.Schedule(voxelCount, 64);

            ModifyOffsetsJob modifyOffsetsJob = new ModifyOffsetsJob()
            {
                resolution = chunkData.Resolution,
                size = tileSize,
                modifiers = chunkData.modifiers,
                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
                normalsX = chunkData.normalsX,
                normalsY = chunkData.normalsY,
            };
            jobHandle = modifyOffsetsJob.Schedule(voxelCount, 64, jobHandle);

            // In principle all dependency jobs should be able to execute at the same time
            JobHandle dependencyGroupHandle = new JobHandle();
            for (int i = 0; i < chunkData.dependencies.Count; i++)
            {
                JobHandle dependencyHandle = chunkData.dependencies[i].ScheduleChunkJob(this, chunkData, jobHandle);
                if (i == 0)
                    dependencyGroupHandle = dependencyHandle;
                else
                    dependencyGroupHandle = JobHandle.CombineDependencies(dependencyHandle, dependencyGroupHandle);
            }

            chunkData.jobHandle = dependencyGroupHandle;
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

            chunkData.modifiers.Clear();
            chunkData.jobHandle = null;
        }
        #endregion Job Scheduling

        #region Modifiers
        public void ModifyGrid(GridModification modification)
        {
            scheduledModifications.Add(modification);
        }
        private void AddScheduledModificationsToChunks()
        {
            for (int i = 0; i < scheduledModifications.Count; i++)
            {
                AddScheduledModificationToChunks(scheduledModifications[i]);
            }
            scheduledModifications.Clear();
        }

        private void AddScheduledModificationToChunks(GridModification modification)
        {
            Rect modificationBounds = modification.GetBounds();
            foreach (var chunkData in chunks)
            {
                Rect chunkBounds = chunkData.Value.GetBounds();
                if (chunkBounds.Intersects(modificationBounds))
                    AddScheduledModificationToChunks(chunkData.Key, chunkData.Value, modification);
            }
        }

        private void AddScheduledModificationToChunks(int2 index, ChunkData chunk, GridModification modification)
        {
            modification.position = modification.position - chunk.Origin;
            chunk.modifiers.Add(modification);

            if (!dirtyChunks.Contains(index))
                dirtyChunks.Add(index);
        }
        #endregion Modifiers

        private void OnDrawGizmos()
        {
            OnDrawGizmosSelected();
            
            if (chunks == null || !drawGizmos)
                return;

            foreach (var chunkData in chunks)
            {
                VoxelGizmos.DrawVoxels(transform, chunkData.Value, tileSize);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (chunks == null)
                return;

            foreach (var chunkData in chunks)
            {
                float2 center = chunkData.Value.Origin + chunkSize * 0.5f;
                Vector3 worldCenter = transform.TransformPoint(new Vector3(center.x, center.y, 0));
                Gizmos.DrawWireCube(worldCenter, new Vector3(tileSize, tileSize, 0) * chunkResolution);
            }
        }
    }
}
