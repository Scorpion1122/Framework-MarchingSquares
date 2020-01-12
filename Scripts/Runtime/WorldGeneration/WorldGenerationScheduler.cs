using Unity.Jobs;
using Unity.Collections;

namespace Thijs.Framework.MarchingSquares
{
    public class WorldGenerationScheduler : IChunkJobScheduler
    {
        public int seed = 1337;

        public float heightOffset = -5f;
        public float heightScale = 10f;
        public NoiseSettings heightNoiseSettings;

        private NoiseSettings[] caveNoiseSettings;
        public float caveNoiseCutOff = 0.5f;

        public bool IsBlocking => true;

        private NativeArray<float> caveNoise;
        private NativeArray<NoiseSettings> caveNoiseInput;

        public void SetCaveNoiseSettings(NoiseSettings[] settings)
        {
            caveNoiseSettings = settings;
        }

        public void OnJobCompleted(ChunkData chunkData)
        {
            // Only need to run world gen once, so remove its dependency
            chunkData.dependencies.Remove(this);

            if (caveNoise.IsCreated)
            {
                caveNoise.Dispose();
                caveNoiseInput.Dispose();
            }
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
                heightScale = heightScale,
                noiseSettings = heightNoiseSettings,

                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
                normalsX = chunkData.normalsX,
                normalsY = chunkData.normalsY,
            };
            dependency = heightGenJob.Schedule(chunkData.fillTypes.Length, 64, dependency);

            caveNoise = new NativeArray<float>(chunkData.fillTypes.Length, Allocator.TempJob);
            caveNoiseInput = new NativeArray<NoiseSettings>(caveNoiseSettings.Length, Allocator.TempJob);
            caveNoiseInput.CopyFrom(caveNoiseSettings);

            NoiseGenerationJob caveNoiseGenerator = new NoiseGenerationJob()
            {
                tileSize = grid.TileSize,
                resolution = chunkData.Resolution,
                origin = chunkData.Origin,

                noiseInput = caveNoiseInput,
                noiseOutput = caveNoise,
            };
            dependency = caveNoiseGenerator.Schedule(caveNoise.Length, 64, dependency);

            WriteNoiseToChunkJob writeNoiseToChunk = new WriteNoiseToChunkJob()
            {
                tileSize = grid.TileSize,
                resolution = chunkData.Resolution,
                noiseCutOff = caveNoiseCutOff,

                fillType = FillType.None,

                noise = caveNoise,

                fillTypes = chunkData.fillTypes,
                offsets = chunkData.offsets,
                normalsX = chunkData.normalsX,
                normalsY = chunkData.normalsY,
            };
            dependency = writeNoiseToChunk.Schedule(caveNoise.Length, 64, dependency);

            return dependency;
        }
    }
}
