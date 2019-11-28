using UnityEditor;

namespace Thijs.Framework.MarchingSquares
{
    public static class TileTerrainEditor
    {
        private static TileTerrainToolbar toolbar;
        
        [InitializeOnLoadMethod]
        private static void InitializeTileTerrainPainter()
        {
            SceneView.duringSceneGui += OnSceneGuiDelegate;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            TileTerrain terrain = null;
            if (Selection.activeGameObject != null)
                terrain = Selection.activeGameObject.GetComponent<TileTerrain>();

            toolbar?.SetActiveTerrain(terrain);
        }

        private static void OnSceneGuiDelegate(SceneView sceneView)
        {
            if (toolbar == null)
            {
                toolbar = new TileTerrainToolbar();
                sceneView.rootVisualElement.Add(toolbar);
                OnSelectionChanged();
            }
        }
    }
}
