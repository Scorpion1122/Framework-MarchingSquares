using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{

    [ExecuteInEditMode]
    public class WorldGeneration : TileTerrainComponent, IChunkJobScheduler
    {
        [SerializeField] private int seed = 1337;

        [Header("Height")]
        [SerializeField] private float heightOffset = -5f;
        [SerializeField] private float heightNoiseFrequency = 5f;
        [SerializeField] private float heightScale = 10f;

        [Header("Rougness")]
        [SerializeField] private float roughnessFrequency = 1f;
        [SerializeField] private float maxRougnessModifier = 2f;
        [SerializeField] private float rougnessHeightScale = 5f;

        public bool IsBlocking => true;

        private void OnEnable()
        {
            TileTerrain.OnChunkInstantiated += OnChunkInitialized;
        }

        private void OnChunkInitialized(int2 index, ChunkData chunkData)
        {
            chunkData.dependencies.Add(this);
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInstantiated -= OnChunkInitialized;
        }

        public JobHandle ScheduleChunkJob(TileTerrain grid, ChunkData chunkData, JobHandle dependency)
        {
            System.Random random = new System.Random(seed);

            HeightGenerationJob heightGenJob = new HeightGenerationJob()
            {
                tileSize = grid.TileSize,
                resolution = chunkData.Resolution,
                origin = chunkData.Origin,

                fillType = FillType.TypeOne,
                heightOffset = heightOffset,
                noiseFrequency = heightNoiseFrequency,
                noiseOffset = random.Next(-10000, 10000),
                heightScale = heightScale,

                roughnessFrequency = roughnessFrequency,
                maxRougnessModifier = maxRougnessModifier,
                rougnessHeightScale = rougnessHeightScale,

                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
                normalsX = chunkData.normalsX,
                normalsY = chunkData.normalsY,
            };
            dependency = heightGenJob.Schedule(chunkData.fillTypes.Length, 64, dependency);

            return dependency;
        }

        public void OnJobCompleted(ChunkData chunkData)
        {
            chunkData.dependencies.Remove(this);
        }
    }
}
