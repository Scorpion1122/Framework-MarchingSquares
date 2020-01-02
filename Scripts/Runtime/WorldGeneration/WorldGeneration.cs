using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{

    [ExecuteInEditMode]
    public class WorldGeneration : TileTerrainComponent
    {
        [SerializeField] private int seed = 1337;

        [Header("Height")]
        [SerializeField] private float heightOffset = -5f;
        [SerializeField] private float heightNoiseFrequency = 5f;
        [SerializeField] private float heightScale = 10f;

        [Header("Height - Rougness")]
        [SerializeField] private float roughnessFrequency = 1f;
        [SerializeField] private float maxRougnessModifier = 2f;
        [SerializeField] private float rougnessHeightScale = 5f;

        [Header("Caves")]
        [SerializeField] private float caveNoiseCutOff = 0.5f;
        [SerializeField] private NoiseSettings[] caveNoiseSettings = new NoiseSettings[1];

        public bool IsBlocking => true;

        private void OnEnable()
        {
            TileTerrain.OnChunkInstantiated += OnChunkInitialized;
        }

        private void OnChunkInitialized(int2 index, ChunkData chunkData)
        {
            WorldGenerationScheduler instance = new WorldGenerationScheduler()
            {
                seed = seed,

                heightOffset = heightOffset,
                heightNoiseFrequency = heightNoiseFrequency,
                heightScale = heightScale,

                roughnessFrequency = roughnessFrequency,
                maxRougnessModifier = maxRougnessModifier,
                rougnessHeightScale = rougnessHeightScale,

                caveNoiseCutOff = caveNoiseCutOff,
            };
            instance.SetCaveNoiseSettings(caveNoiseSettings);
            chunkData.dependencies.Add(instance);
        }

        private void OnDisable()
        {
            TileTerrain.OnChunkInstantiated -= OnChunkInitialized;
        }
    }
}
