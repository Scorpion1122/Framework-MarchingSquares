using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [CustomEditor(typeof(TileTerrain))]
    public class TileTerrainEditor : Editor
    {
        private ModifierShape modifierShape = ModifierShape.Circle;
        private FillType fillType = FillType.TypeOne;
        private float modifierSize = 0.5f;

        private bool didPress;

        private TileTerrain tileTerrain;

        private void OnEnable()
        {
            tileTerrain = (TileTerrain) target;
            Tools.hidden = true;
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical("box");
            {
                modifierShape = (ModifierShape) EditorGUILayout.EnumPopup("Brush", modifierShape);
                fillType = (FillType) EditorGUILayout.EnumPopup("Type", fillType);
                modifierSize = EditorGUILayout.FloatField("Size", modifierSize);
            }
            GUILayout.EndVertical();

            base.OnInspectorGUI();
        }

        private void OnSceneGUI()
        {
            Vector3 handlePosition;
            if (!GetMousePositionOnGrid(out handlePosition))
                return;

            if (modifierShape == ModifierShape.Circle)
            {
                Handles.DrawWireDisc(handlePosition, tileTerrain.transform.forward, modifierSize);
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
                Vector3 localPosition = tileTerrain.transform.InverseTransformPoint(handlePosition);
                GridModification modification = new GridModification()
                {
                    ModifierShape = modifierShape,
                    position = new float2(localPosition.x, localPosition.y),
                    setFilltype = fillType,
                    size = modifierSize,
                };
                tileTerrain.ModifyGrid(modification);
                EditorApplication.QueuePlayerLoopUpdate();
                Event.current.Use();
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (Event.current.type == EventType.MouseMove)
                SceneView.RepaintAll();
        }

        private bool GetMousePositionOnGrid(out Vector3 position)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Plane plane = new Plane(tileTerrain.transform.forward, tileTerrain.transform.position);

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
