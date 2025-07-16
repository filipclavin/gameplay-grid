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
        private Vector3Int _lastSelectedCell = -Vector3Int.one;

        private HashSet<int> _hiddenX = new();
        private HashSet<int> _hiddenY = new();
        private HashSet<int> _hiddenZ = new();

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

            private bool isLayerVisibilityOpen = false;

            public Grid3DToolOverlay(Grid3DTool tool)
            {
                _tool = tool;
                _grid = tool._grid;
            }

            public override VisualElement CreatePanelContent()
            {
                VisualElement root = new();
                root.style.minWidth = 250;

                root.Add(GenerateVisibilityController());

                if (_tool._selectedCells.Count == 0)
                {
                    Label noSelectionLabel = new("No cells selected");
                    root.Add(noSelectionLabel);
                    return root;
                }

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
                    value           = nodeFactoriesMixed ? null : _grid.TryGetNode(someSelectedCell)?.NodeFactory,
                    objectType      = typeof(NodeFactory),
                    showMixedValue  = nodeFactoriesMixed
                };

                // Set up the callback for when the node factory is changed
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

                root.Add(nodeFactoryField);

                return root;
            }

            private Foldout GenerateVisibilityController()
            {
                Foldout visibilityController = new() { value = isLayerVisibilityOpen, text = "Layer Visibility" };
                visibilityController.RegisterValueChangedCallback(evt =>
                {
                    isLayerVisibilityOpen = evt.newValue;
                });

                visibilityController.Add(GenerateAxisVisibilityController(Axis.X));
                visibilityController.Add(GenerateAxisVisibilityController(Axis.Y));
                visibilityController.Add(GenerateAxisVisibilityController(Axis.Z));

                return visibilityController;
            }

            private VisualElement GenerateAxisVisibilityController(Axis axis)
            {
                string axisName = axis switch
                {   Axis.X => "X",
                    Axis.Y => "Y",
                    Axis.Z => "Z",
                    _ => throw new System.ArgumentOutOfRangeException(nameof(axis), axis, null)
                };

                int axisSize = axis switch
                {   Axis.X => _grid.Dimensions.x,
                    Axis.Y => _grid.Dimensions.y,
                    Axis.Z => _grid.Dimensions.z,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(axis), axis, null)
                };

                ref HashSet<int> GetHiddenSet()
                {
                    if (axis == Axis.X) return ref _tool._hiddenX;
                    if (axis == Axis.Y) return ref _tool._hiddenY;
                    return ref _tool._hiddenZ;
                }
                ref HashSet<int> hiddenAxis = ref GetHiddenSet();

                void ClearHiddenSet()
                {
                    if (axis == Axis.X)
                        _tool._hiddenX.Clear();
                    else if (axis == Axis.Y)
                        _tool._hiddenY.Clear();
                    else
                        _tool._hiddenZ.Clear();

                    _tool.RefreshOverlay();
                }

                void AddToHiddenSet(int index)
                {
                    if (axis == Axis.X)
                    {
                        _tool._hiddenX.Add(index);
                        _tool._selectedCells.RemoveWhere(cell => cell.x == index);
                    }
                    else if (axis == Axis.Y)
                    {
                        _tool._hiddenY.Add(index);
                        _tool._selectedCells.RemoveWhere(cell => cell.y == index);
                    }
                    else
                    {
                        _tool._hiddenZ.Add(index);
                        _tool._selectedCells.RemoveWhere(cell => cell.z == index);
                    }

                    _tool.RefreshOverlay();
                }

                void RemoveFromHiddenSet(int index)
                {
                    if (axis == Axis.X)
                        _tool._hiddenX.Remove(index);
                    else if (axis == Axis.Y)
                        _tool._hiddenY.Remove(index);
                    else
                        _tool._hiddenZ.Remove(index);

                    _tool.RefreshOverlay();
                }

                VisualElement container = new();
                container.style.flexDirection = FlexDirection.Row;
                container.style.alignItems = Align.FlexStart;
                container.style.height = 32;

                Toggle toggleAll = new(axisName)
                {
                    value = hiddenAxis.Count == 0
                };
                toggleAll.labelElement.style.minWidth = 0;
                container.Add(toggleAll);

                ScrollView scrollView = new() { mode = ScrollViewMode.Horizontal };
                scrollView.style.maxWidth = 200;
                for (int i = 0; i < axisSize; i++)
                {
                    int index = i;
                    Toggle toggle = new($"{index}")
                    {
                        value = !hiddenAxis.Contains(index)
                    };
                    toggle.labelElement.style.minWidth = 0;
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue)
                            RemoveFromHiddenSet(index);
                        else
                        {
                            AddToHiddenSet(index);
                        }
                    });
                    scrollView.Add(toggle);
                }
                container.Add(scrollView);

                toggleAll.RegisterValueChangedCallback(evt =>
                {
                    foreach (VisualElement child in scrollView.Children())
                    {
                        Toggle toggle = child as Toggle;

                        if (toggle != null)
                        {
                            toggle.value = evt.newValue;
                        }
                    }

                    if (evt.newValue)
                        ClearHiddenSet();
                    else
                    {
                        for (int i = 0; i < axisSize; i++)
                        {
                            AddToHiddenSet(i);
                        }
                    }
                });

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
                if (_hiddenX.Contains(x)) continue;
                for (int y = 0; y < _grid.Dimensions.y; y++)
                {
                    if (_hiddenY.Contains(y)) continue;
                    for (int z = 0; z < _grid.Dimensions.z; z++)
                    {
                        if (_hiddenZ.Contains(z)) continue;

                        Vector3Int cell = new(x, y, z);
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
                        if (_hiddenX.Contains(x)) continue;
                        for (int y = 0; y < _grid.Dimensions.y; y++)
                        {
                            if (_hiddenY.Contains(y)) continue;
                            for (int z = 0; z < _grid.Dimensions.z; z++)
                            {
                                if (_hiddenZ.Contains(z)) continue;

                                Vector3Int cell = new(x, y, z);
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
            if (_event.shift && _event.control)
            {
                Vector3Int lastSelected = _lastSelectedCell == -Vector3Int.one ? cell : _lastSelectedCell;

                Vector3Int min = Vector3Int.Min(lastSelected, cell);
                Vector3Int max = Vector3Int.Max(lastSelected, cell);

                List<Vector3Int> newSelection = new();
                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            newSelection.Add(new(x, y, z));
                        }
                    }
                }
                SelectCells(newSelection, false);
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

                List<Vector3Int> newSelection = new();
                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            newSelection.Add(new(x, y, z));
                        }
                    }
                }
                SelectCells(newSelection, true);
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

        private void RefreshOverlay()
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

            _lastSelectedCell = cell;
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

            _lastSelectedCell = cells.Count > 0 ? cells[^1] : -Vector3Int.one;
            RefreshOverlay();
        }

        private void ToggleCell(Vector3Int cell)
        {
            if (_selectedCells.Contains(cell))
            {
                _selectedCells.Remove(cell);
            }
            else
            {
                _selectedCells.Add(cell);
                _lastSelectedCell = cell;
            }

            RefreshOverlay();
        }

        private void ClearSelectedCells()
        {
            _selectedCells.Clear();
            _lastSelectedCell = -Vector3Int.one;
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
