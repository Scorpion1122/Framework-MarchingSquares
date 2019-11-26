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
            // Only enable this tool when we are in 2D mode
            if (!sceneView.orthographic)
                return;

        }
    }
}