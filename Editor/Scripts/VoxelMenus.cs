using UnityEditor;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public static class VoxelMenus
    {
        [MenuItem("Voxel/Create Terrain")]
        private static void CreateNewVoxelTerrain()
        {
            GameObject gameObject = new GameObject("Voxel Terrain");
            gameObject.AddComponent<VoxelGrid>();
        }
    }
}