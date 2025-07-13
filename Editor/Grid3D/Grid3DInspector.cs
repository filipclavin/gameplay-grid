using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GameplayGrid;

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

            return root;
        }
    }
}
