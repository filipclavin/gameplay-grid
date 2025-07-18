using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using GameplayGrid;
using UnityEditor.EditorTools;

namespace GameplayGridEditor
{
    [CustomEditor(typeof(Grid3D))]
    public class Grid3DInspector : Editor
    {
        Grid3D _grid;

        public override VisualElement CreateInspectorGUI()
        {
            _grid = target as Grid3D;

            VisualElement root = new();

            Vector3IntField dimensionsField = new("Dimensions")
            {
                value = _grid.Dimensions,
            };
            dimensionsField.RegisterValueChangedCallback(evt =>
            {
                Vector3Int clampedValue = Vector3Int.Max(evt.newValue, Vector3Int.one);
                dimensionsField.value = clampedValue;

                _grid.SetDimensions(clampedValue);
                EditorUtility.SetDirty(_grid);

                // Refresh the tool to reflect changes in overlay
                ToolManager.RestorePreviousPersistentTool();
                ToolManager.SetActiveTool<Grid3DTool>();
            });
            dimensionsField.AddToClassList("unity-base-field__aligned");
            root.Add(dimensionsField);

            Button openEditorButton = new() { text = "Edit Grid" };
            openEditorButton.clicked += () =>
            {
                ToolManager.SetActiveTool<Grid3DTool>();
            };
            root.Add(openEditorButton);

            return root;
        }
    }
}
