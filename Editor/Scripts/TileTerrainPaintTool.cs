using UnityEditor;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public static class TileTerrainPaintTool
    {
        [InitializeOnLoadMethod]
        private static void InitializeTileTerrainPainter()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
        }
    }
}