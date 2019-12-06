using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public class TilePainterCursor
    {
        private bool didPress;
        
        public void DrawCursor(TileTerrainToolbar toolbar)
        {
            if (toolbar.Terrain == null)
                return;
            
            Vector3 handlePosition;
            if (!GetMousePositionOnGrid(toolbar.Terrain, out handlePosition))
                return;

            float modifierSize = toolbar.SelectedSize;
            FillType fillType = toolbar.SelectedFillType;
            ModifierShape modifierShape = toolbar.SelectedShape;
            
            if (modifierShape == ModifierShape.Circle)
            {
                Handles.DrawWireDisc(handlePosition, toolbar.Terrain.transform.forward, modifierSize);
            }
            else if (modifierShape == ModifierShape.Square)
            {
                Handles.DrawWireCube(handlePosition, new Vector3(modifierSize, modifierSize, 0f) * 2f);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                didPress = true;
            }

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                didPress = false;
            }

            if (didPress && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
            {
                Vector3 localPosition = toolbar.Terrain.transform.InverseTransformPoint(handlePosition);
                GridModification modification = new GridModification()
                {
                    ModifierShape = modifierShape,
                    position = new float2(localPosition.x, localPosition.y),
                    modifierType = toolbar.SelectedType,
                    setFilltype = fillType,
                    size = modifierSize,
                };
                toolbar.Terrain.ModifyGrid(modification);
                EditorApplication.QueuePlayerLoopUpdate();
                Event.current.Use();
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (Event.current.type == EventType.MouseMove)
                SceneView.RepaintAll();
        }
        
        private static bool GetMousePositionOnGrid(TileTerrain terrain, out Vector3 position)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Plane plane = new Plane(terrain.transform.forward, terrain.transform.position);

            float distnance;
            if (plane.Raycast(ray, out distnance))
            {
                position = ray.origin + ray.direction.normalized * distnance;
                return true;
            }

            position = Vector3.zero;
            return false;
        }
    }
}
