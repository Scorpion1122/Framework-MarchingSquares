using UnityEditor;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public static class TileTerrainMenus
    {
        [MenuItem("Tile Terrain/Create Terrain")]
        private static void CreateNewVoxelTerrain()
        {
            GameObject gameObject = new GameObject("Tile Terrain");
            gameObject.AddComponent<TileTerrain>();
            gameObject.AddComponent<TileTerrainRenderer>();
            gameObject.AddComponent<TileTerrainCollider>();
        }
    }
}