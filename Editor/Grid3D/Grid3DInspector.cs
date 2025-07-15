using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
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

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

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
