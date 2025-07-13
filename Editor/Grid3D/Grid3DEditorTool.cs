using UnityEditor.EditorTools;
using UnityEngine;
using GameplayGrid;
using System.Collections.Generic;
using Codice.Client.Common;
using UnityEditor.ShortcutManagement;
using UnityEditor;

namespace GameplayGridEditor
{
    [EditorTool("Grid3D Editor Tool", typeof(Grid3D))]
    public class Grid3DEditorTool : EditorTool
    {
        private static readonly float cellHandleSize = .2f;
        private static readonly float cellHandleOpacity = .2f;
        private static readonly float selectedCellHandleOpacity = .5f;

        private Grid3D _grid;

        private List<Vector3Int> _selectedCells = new();
        private List<Vector3Int> _hiddenCells = new();

        [Shortcut("Activate Grid3D Editor Tool", null, KeyCode.G, ShortcutModifiers.Control)]
        private static void Grid3DEditorToolShortcut()
        {
            if (Selection.GetFiltered<Grid3D>(SelectionMode.TopLevel).Length > 0)
                ToolManager.SetActiveTool<Grid3DEditorTool>();
        }

        public override void OnActivated()
        {
            _grid = target as Grid3D;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            for (int x = 0; x < _grid.Dimensions.x; x++)
            {
                for (int y = 0; y < _grid.Dimensions.y; y++)
                {
                    for (int z = 0; z < _grid.Dimensions.z; z++)
                    {
                        Vector3Int coordinates = new(x, y, z);
                        bool isSelected = _selectedCells.Contains(coordinates);

                        int controlID = GUIUtility.GetControlID(FocusType.Passive);
                        Handles.color = new Color(1f, 1f, 1f, isSelected ? selectedCellHandleOpacity : cellHandleOpacity);
                        Handles.CubeHandleCap(controlID, _grid.CoordinateToWorldPosition(coordinates), _grid.transform.rotation, cellHandleSize, Event.current.type);

                        HandleCellClick(controlID, coordinates);
                    }
                }
            }
        }

        private void HandleCellClick(int controlID, Vector3Int coordinates)
        {
            if (HandleUtility.nearestControl != controlID)
                return;

            Event e = Event.current;
            EventType eventType = e.GetTypeForControl(controlID);

            switch (eventType)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (_selectedCells.Contains(coordinates))
                        {
                            _selectedCells.Remove(coordinates);
                        }
                        else
                        {
                            _selectedCells.Add(coordinates);
                        }
                        e.Use();
                    }
                    break;
            }
        }
    }
}
