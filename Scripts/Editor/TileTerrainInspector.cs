using UnityEditor;

namespace Thijs.Framework.MarchingSquares
{
    [CustomEditor(typeof(TileTerrain))]
    public class TileTerrainInspector : Editor
    {
        private void OnEnable()
        {
            Tools.hidden = true;
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }
    }
}
