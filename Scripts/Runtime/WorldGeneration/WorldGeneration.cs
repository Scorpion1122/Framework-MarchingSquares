using Unity.Jobs;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public class WorldGeneration : MonoBehaviour
    {
        [SerializeField] private int seed = 1337;

        [Header("Height")]
        [SerializeField] private float heightNoiseFrequency = 5f;
        [SerializeField] private float heightScale = 10f;

        [Header("Rougness")]
        [SerializeField] private float roughnessFrequency = 1f;
        [SerializeField] private float maxRougnessModifier = 2f;
        [SerializeField] private float rougnessHeightScale = 5f;

        public void GenerateChunkData(TileTerrain grid, ChunkData chunk)
        {
            System.Random random = new System.Random(seed);

            HeightGenerationJob heightGenJob = new HeightGenerationJob()
            {
                tileSize = grid.TileSize,
                resolution = chunk.Resolution,
                origin = chunk.Origin,

                fillType = FillType.TypeOne,
                noiseFrequency = heightNoiseFrequency,
                noiseOffset = random.Next(-10000, 10000),
                heightScale = heightScale,

                roughnessFrequency = roughnessFrequency,
                maxRougnessModifier = maxRougnessModifier,
                rougnessHeightScale = rougnessHeightScale,

                fillTypes = chunk.fillTypes,
                offsets = chunk.offsets,
                normalsX = chunk.normalsX,
                normalsY = chunk.normalsY,
            };
            JobHandle handle = heightGenJob.Schedule(chunk.fillTypes.Length, 64);

            handle.Complete();
        }
    }
}
