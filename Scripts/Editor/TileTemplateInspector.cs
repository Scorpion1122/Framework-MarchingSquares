using System;
using UnityEditor;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    [CustomEditor(typeof(TileTemplate))]
    public class TileTemplateInspector : Editor
    {
        private string[] enumNames;
        private TileTemplate tileTemplate;

        private void OnEnable()
        {
            enumNames = Enum.GetNames(typeof(FillType));
            tileTemplate = (TileTemplate) target;
        }

        public override void OnInspectorGUI()
        {
            DrawPropertyArray(serializedObject.FindProperty("names"), true);
            DrawPropertyArray(serializedObject.FindProperty("materials"));
            DrawPropertyArray(serializedObject.FindProperty("physicsMaterials"));
            DrawPropertyArray(serializedObject.FindProperty("layers"));
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPropertyArray(SerializedProperty property, bool includeFirst = false)
        {
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName);
            if (!property.isExpanded)
                return;

            if (property.arraySize != enumNames.Length)
                property.arraySize = enumNames.Length;

            EditorGUI.indentLevel = EditorGUI.indentLevel + 1;
            int index = (includeFirst) ? 0 : 1;
            for (int i = index; i < property.arraySize; i++)
            {
                SerializedProperty itemProperty = property.GetArrayElementAtIndex(i);
                string label = tileTemplate.GetName((FillType) i);
                EditorGUILayout.PropertyField(itemProperty, new GUIContent(label));
            }
            EditorGUI.indentLevel = EditorGUI.indentLevel - 1;
        }
    }
}