using GameplayGrid;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;

namespace GameplayGridEditor
{
    [EditorTool("Grid3D Editor Tool", typeof(Grid3D))]
    public class Grid3DTool : EditorTool
    {
        private static readonly float s_cellHandleSize = .2f;
        private static readonly Color s_cellHandleColor = new(1f, 1f, 1f, .2f);
        private static readonly Color s_cellHandleColorEnabled = new(0f, 1f, 0f, .4f);
        private static readonly Color s_cellHandleColorDisabled = new(1f, 0f, 0f, .4f);
        private static readonly Color s_cellHandleColorSelected = new(.6f, .8f, 1f, .4f);

        private static readonly Color s_selectionRectangleColor = new(.5f, .55f, .65f, 0.15f);
        private static readonly Color s_selectionRectangleOutlineColor = new(.5f, .55f, .65f, .75f);

        private static readonly float s_minimumDragDistance = 5f;

        private Grid3D _grid;

        private HashSet<Vector3Int> _selectedCells = new();
        private Vector3Int _shiftAnchor = -Vector3Int.one;

        private Event _event;

        private Vector2 _mouseDownPosition;
        private bool _isDragging;
        private Rect _selectionRect = new();

        private SceneView _sceneView;

        [Overlay(defaultDisplay = true)]
        class Grid3DToolOverlay : Overlay
        {
            private Grid3DTool  _tool;
            private Grid3D      _grid;

            public Grid3DToolOverlay(Grid3DTool tool)
            {
                _tool = tool;
                _grid = tool._grid;
                displayName = "Grid3D Editor";
            }

            public override VisualElement CreatePanelContent()
            {
                VisualElement root = new();

                if (_tool._selectedCells.Count == 0)
                    root.Add(new Label("No cells selected"));
                else
                    root.Add(CreateNodeFactoryField());

                root.Add(CreateVisibilityController());

                return root;
            }

            private ObjectField CreateNodeFactoryField()
            {
                Vector3Int someSelectedCell = -Vector3Int.one;
                foreach (var cell in _tool._selectedCells)
                {
                    someSelectedCell = cell;
                    break;
                }
                Node someSelectedNode = _grid.TryGetNode(someSelectedCell);

                bool nodeFactoriesMixed = false;
                foreach (var cell in _tool._selectedCells)
                {
                    if (nodeFactoriesMixed) break;

                    Node node = _grid.TryGetNode(cell);

                    if (node == null)
                    {
                        if (someSelectedNode == null)
                            continue;
                        else
                        {
                            nodeFactoriesMixed = true;
                            break;
                        }
                    }

                    if (someSelectedNode == null)
                    {
                        nodeFactoriesMixed = true;
                        break;
                    }

                    if (node.NodeFactory != someSelectedNode.NodeFactory)
                    {
                        nodeFactoriesMixed = true;
                    }
                }

                ObjectField nodeFactoryField = new("Node Factory")
                {
                    value = nodeFactoriesMixed ? null : _grid.TryGetNode(someSelectedCell)?.NodeFactory,
                    objectType = typeof(NodeFactory),
                    showMixedValue = nodeFactoriesMixed
                };

                nodeFactoryField.RegisterValueChangedCallback(evt =>
                {
                    NodeFactory newFactory = evt.newValue as NodeFactory;

                    foreach (var cell in _tool._selectedCells)
                    {
                        if (newFactory == null)
                            _grid.TrySetNode(cell, null);
                        else
                            _grid.TrySetNode(cell, newFactory.CreateNode(_grid, cell));
                    }

                    EditorUtility.SetDirty(_grid);
                });

                return nodeFactoryField;
            }

            private VisualElement CreateVisibilityController()
            {
                VisualElement container = new();
                container.style.flexDirection = FlexDirection.Row;
                container.style.justifyContent = Justify.Center;

                Button hideSelected = new() { text = "Hide selected" };
                hideSelected.style.flexGrow = 1;
                hideSelected.clicked += () =>
                {
                    foreach (var cell in _tool._selectedCells)
                    {
                        _grid.HiddenCells.Add(cell);
                    }
                    _tool._selectedCells.Clear();
                    _tool.RefreshOverlay();
                    EditorUtility.SetDirty(_grid);
                };
                container.Add(hideSelected);

                Button hideDeselected = new() { text = "Hide deselected" };
                hideDeselected.style.flexGrow = 1;
                hideDeselected.clicked += () =>
                {
                    for (int x = 0; x < _grid.Dimensions.x; x++)
                    {
                        for (int y = 0; y < _grid.Dimensions.y; y++)
                        {
                            for (int z = 0; z < _grid.Dimensions.z; z++)
                            {
                                Vector3Int cell = new(x, y, z);
                                if (_tool._selectedCells.Contains(cell)) continue;

                                _grid.HiddenCells.Add(cell);
                            }
                        }
                    }
                    EditorUtility.SetDirty(_grid);
                };
                container.Add(hideDeselected);

                Button showAll = new() { text = "Show all" };
                showAll.style.flexGrow = 1;
                showAll.clicked += () =>
                {
                    _grid.HiddenCells.Clear();
                    EditorUtility.SetDirty(_grid);
                };
                container.Add(showAll);

                return container;
            }
        }

        private Grid3DToolOverlay _overlay;

        public override void OnActivated()
        {
            _grid = target as Grid3D;
            _sceneView = SceneView.lastActiveSceneView;
            SceneView.AddOverlayToActiveView(_overlay = new(this));
        }

        public override void OnWillBeDeactivated()
        {
            SceneView.RemoveOverlayFromActiveView(_overlay);
        }

        public override void OnToolGUI(EditorWindow window)
        {
            _event = Event.current;

            bool isEventUsed = HandleDrag();

            bool wasAnyCellClicked = false;
            for (int x = 0; x < _grid.Dimensions.x; x++)
            {
                for (int y = 0; y < _grid.Dimensions.y; y++)
                {
                    for (int z = 0; z < _grid.Dimensions.z; z++)
                    {
                        Vector3Int cell = new(x, y, z);
                        if (_grid.HiddenCells.Contains(cell)) continue;

                        Vector3 center = _grid.CellToWorldPosition(cell);
                        if (!IsWorldPointVisible(center, _sceneView)) continue;

                        int controlID = GUIUtility.GetControlID(FocusType.Passive);

                        Handles.color = GetCellHandleColor(cell);
                        Handles.SphereHandleCap(controlID, center, _grid.transform.rotation, s_cellHandleSize, Event.current.type);

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
                ClearSelectedCells();
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

                    List<Vector3Int> newSelection = new();
                    for (int x = 0; x < _grid.Dimensions.x; x++)
                    {
                        for (int y = 0; y < _grid.Dimensions.y; y++)
                        {
                            for (int z = 0; z < _grid.Dimensions.z; z++)
                            {
                                Vector3Int cell = new(x, y, z);
                                if (_grid.HiddenCells.Contains(cell)) continue;

                                Vector3 center = _grid.CellToWorldPosition(cell);

                                Vector2 cellGUIPoint = HandleUtility.WorldToGUIPoint(center);

                                if (_selectionRect.Contains(cellGUIPoint))
                                    newSelection.Add(cell);
                            }
                        }
                    }

                    if (_event.control)
                    {
                        if (newSelection.Count > 0)
                            SelectCells(newSelection, false);
                    }
                    else
                        SelectCells(newSelection, true);

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
            if (_event.shift && _shiftAnchor != -Vector3Int.one)
            {
                Vector3Int min = Vector3Int.Min(_shiftAnchor, cell);
                Vector3Int max = Vector3Int.Max(_shiftAnchor, cell);

                List<Vector3Int> newSelection = new();
                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            Vector3Int c = new(x, y, z);
                            if (_grid.HiddenCells.Contains(cell)) continue;

                            newSelection.Add(c);
                        }
                    }
                }
                SelectCells(newSelection, !_event.control);
            }
            else if (_event.control)
            {
                ToggleCell(cell);
            }
            else
            {
                SelectCell(cell, true);
            }

            _event.Use();
            return true;
        }

        public void RefreshOverlay()
        {
            _overlay.displayed = false;
            _overlay.displayed = true;
        }

        private void SelectCell(Vector3Int cell, bool clearPreviousSelection)
        {
            if (clearPreviousSelection)
            {
                _selectedCells.Clear();
            }

            if (!_selectedCells.Contains(cell))
            {
                _selectedCells.Add(cell);
            }
            
            _shiftAnchor = cell;

            RefreshOverlay();
        }

        private void SelectCells(List<Vector3Int> cells, bool clearPreviousSelection)
        {
            if (clearPreviousSelection)
            {
                _selectedCells.Clear();
            }

            foreach (var cell in cells)
            {
                if (!_selectedCells.Contains(cell))
                {
                    _selectedCells.Add(cell);
                }
            }

            RefreshOverlay();
        }

        private void ToggleCell(Vector3Int cell)
        {
            if (_selectedCells.Contains(cell))
            {
                _selectedCells.Remove(cell);
                _shiftAnchor = -Vector3Int.one;
            }
            else
            {
                _selectedCells.Add(cell);
                _shiftAnchor = cell;
            }

            RefreshOverlay();
        }

        private void ClearSelectedCells()
        {
            _selectedCells.Clear();
            _shiftAnchor = -Vector3Int.one;
            RefreshOverlay();
        }

        private static bool IsWorldPointVisible(Vector3 worldPos, SceneView sceneView)
        {
            Vector3 viewportPoint = sceneView.camera.WorldToViewportPoint(worldPos);
            return viewportPoint.z > 0 &&
                   viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1;
        }

        private Color GetCellHandleColor(Vector3Int cell)
        {
            Node node = _grid.TryGetNode(cell);

            if (_selectedCells.Contains(cell))
                return s_cellHandleColorSelected;
            else if (node == null)
                return s_cellHandleColor;
            else if (node.IsEnabled)
                return s_cellHandleColorEnabled;
            else
                return s_cellHandleColorDisabled;
        }

        [Shortcut("Activate Grid3D Editor Tool", null, KeyCode.G, ShortcutModifiers.Control)]
        private static void Grid3DEditorToolShortcut()
        {
            if (Selection.GetFiltered<Grid3D>(SelectionMode.TopLevel).Length > 0)
                ToolManager.SetActiveTool<Grid3DTool>();
        }
    }
}
