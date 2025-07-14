using UnityEditor.EditorTools;
using UnityEngine;
using GameplayGrid;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEditor;

namespace GameplayGridEditor
{
    [EditorTool("Grid3D Editor Tool", typeof(Grid3D))]
    public class Grid3DEditorTool : EditorTool
    {
        private static readonly float s_cellHandleSize  = .2f;
        private static readonly Color s_cellHandleColor = new(1f, 1f, 1f, .2f);

        private static readonly Color s_selectionRectangleColor         = new(.5f, .55f, .65f, 0.15f);
        private static readonly Color s_selectionRectangleOutlineColor  = new(.5f, .55f, .65f, .75f);

        private static readonly float s_minimumDragDistance = 5f;

        private Grid3D _grid;

        private HashSet<Vector3Int> _selectedCells      = new();
        private Vector3Int          _lastSelectedCell   = -Vector3Int.one;

        private HashSet<int> _hiddenX = new();
        private HashSet<int> _hiddenY = new();
        private HashSet<int> _hiddenZ = new();

        private Event _event;

        private Vector2 _mouseDownPosition;
        private bool _isDragging;
        private Rect _selectionRect = new();

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
            _event = Event.current;
            
            bool isEventUsed = HandleDrag();

            SceneView sceneView = window as SceneView;
            if (sceneView == null) return;

            bool wasAnyCellClicked = false;
            for (int x = 0; x < _grid.Dimensions.x; x++)
            {
                if (_hiddenX.Contains(x)) continue;
                for (int y = 0; y < _grid.Dimensions.y; y++)
                {
                    if (_hiddenY.Contains(y)) continue;
                    for (int z = 0; z < _grid.Dimensions.z; z++)
                    {
                        if (_hiddenZ.Contains(z)) continue;

                        Vector3Int cell = new(x, y, z);
                        Vector3 center = _grid.CoordinatesToWorldPosition(cell);
                        if (!IsPointVisible(center, sceneView)) continue;

                        bool isSelected = _selectedCells.Contains(cell);

                        int controlID = GUIUtility.GetControlID(FocusType.Passive);
                        Handles.color = s_cellHandleColor;
                        Handles.SphereHandleCap(controlID, center, _grid.transform.rotation, s_cellHandleSize, Event.current.type);

                        if (isSelected)
                        {
                            Handles.color = Color.orange;
                            Handles.DrawWireCube(center, Vector3.one * s_cellHandleSize);
                        }

                        if (!isEventUsed)
                        {
                            if (!_isDragging && _event.type == EventType.MouseUp && _event.button == 0 && HandleUtility.nearestControl == controlID)
                            {
                                isEventUsed = HandleCellClick(controlID, cell);
                                wasAnyCellClicked = true;
                            }
                        }
                    }
                }
            }

            if (!isEventUsed && _event.type == EventType.MouseUp && _event.button == 0 && !wasAnyCellClicked && _selectedCells.Count > 0)
            {
                _selectedCells.Clear();
                _event.Use();
            }
        }

        private bool HandleDrag()
        {
            if (_event.button != 0 || (_isDragging && _event.alt)) return false;

            switch (_event.type)
            {
                case EventType.MouseDown:
                    _mouseDownPosition = _event.mousePosition;
                    return false;
                case EventType.MouseDrag:
                    _isDragging = Vector2.Distance(_mouseDownPosition, _event.mousePosition) >= s_minimumDragDistance;
                    if (!_isDragging)
                    {
                        return false;
                    }

                    _selectionRect = new(
                        Mathf.Min(_mouseDownPosition.x, _event.mousePosition.x),
                        Mathf.Min(_mouseDownPosition.y, _event.mousePosition.y),
                        Mathf.Abs(_mouseDownPosition.x - _event.mousePosition.x),
                        Mathf.Abs(_mouseDownPosition.y - _event.mousePosition.y));

                    HashSet<Vector3Int> newSelection = new();
                    for (int x = 0; x < _grid.Dimensions.x; x++)
                    {
                        if (_hiddenX.Contains(x)) continue;
                        for (int y = 0; y < _grid.Dimensions.y; y++)
                        {
                            if (_hiddenY.Contains(y)) continue;
                            for (int z = 0; z < _grid.Dimensions.z; z++)
                            {
                                if (_hiddenZ.Contains(z)) continue;

                                Vector3Int cell = new(x, y, z);

                                Vector2 cellGUIPoint = HandleUtility.WorldToGUIPoint(_grid.CoordinatesToWorldPosition(cell));

                                if (_selectionRect.Contains(cellGUIPoint))
                                    newSelection.Add(cell);
                            }
                        }
                    }

                    if (_event.control)
                    {
                        foreach (var selectedCell in newSelection)
                        {
                            if (!_selectedCells.Contains(selectedCell))
                                _selectedCells.Add(selectedCell);
                        }
                    }
                    else
                    {
                        _selectedCells = newSelection;
                    }

                        _event.Use();
                    return true;
                case EventType.MouseUp:
                    if (_isDragging)
                    {
                        _isDragging = false;
                        _event.Use();
                        return true;
                    }
                    _isDragging = false;
                    break;
            }

            if (_isDragging)
            {
                Handles.BeginGUI();
                Handles.DrawSolidRectangleWithOutline(_selectionRect, s_selectionRectangleColor, s_selectionRectangleOutlineColor);
                Handles.EndGUI();
            }

            return false;
        }

        private bool HandleCellClick(int controlID, Vector3Int cell)
        {
            if (_event.shift && _event.control)
            {
                Vector3Int lastSelected = _lastSelectedCell == -Vector3Int.one ? cell : _lastSelectedCell;

                Vector3Int min = Vector3Int.Min(lastSelected, cell);
                Vector3Int max = Vector3Int.Max(lastSelected, cell);

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            Vector3Int c = new(x, y, z);
                            if (!_selectedCells.Contains(c))
                                _selectedCells.Add(c);
                        }
                    }
                }
            }
            else if (_event.shift)
            {
                Vector3Int furthestSelected = cell;
                foreach (var selected in _selectedCells)
                {
                    if (Vector3Int.Distance(selected, cell) > Vector3Int.Distance(furthestSelected, cell))
                    {
                        furthestSelected = selected;
                    }
                }

                Vector3Int min = Vector3Int.Min(furthestSelected, cell);
                Vector3Int max = Vector3Int.Max(furthestSelected, cell);

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            Vector3Int c = new(x, y, z);
                            if (!_selectedCells.Contains(c))
                                _selectedCells.Add(c);
                        }
                    }
                }
            }
            else if (_event.control)
            {
                if (_selectedCells.Contains(cell))
                    _selectedCells.Remove(cell);
                else
                    _selectedCells.Add(cell);
            }
            else
            {
                _selectedCells.Clear();
                _selectedCells.Add(cell);
            }

            _event.Use();
            return true;
        }

        private static bool IsPointVisible(Vector3 worldPos, SceneView sceneView)
        {
            Vector3 viewportPoint = sceneView.camera.WorldToViewportPoint(worldPos);
            return viewportPoint.z > 0 &&
                   viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1;
        }
    }
}
