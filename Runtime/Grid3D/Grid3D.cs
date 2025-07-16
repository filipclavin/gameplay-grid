using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameplayGrid
{
    public class Grid3D : MonoBehaviour, ISerializationCallbackReceiver
    {
        [field: SerializeField, Min(1)] public Vector3Int Dimensions { get; private set; } = new(1, 1, 1);

        [SerializeReference] private List<Node> _nodeMatrix = new() { null };

        private static Vector3 cellOffset = new(0.5f, 0.5f, 0.5f);

#if UNITY_EDITOR
        public HashSet<Vector3Int> HiddenCells = new();
        [SerializeField] private List<Vector3Int> _serializableHiddenCells = new();
#endif

        public void SetDimensions(int x, int y, int z)
        {
            SetDimensions(new Vector3Int(x, y, z));
        }

        public void SetDimensions(Vector3Int newDimensions)
        {
            Assert.IsTrue(newDimensions.x > 0 && newDimensions.y > 0 && newDimensions.z > 0, "Grid dimensions must be greater than zero.");

            List<Node> newNodeMatrix = new(newDimensions.x * newDimensions.y * newDimensions.z);
            for (int i = 0; i < newDimensions.x * newDimensions.y * newDimensions.z; i++)
            {
                newNodeMatrix.Add(null);
            }

            Vector3Int min = Vector3Int.Min(Dimensions, newDimensions);

            for (int z = 0; z < min.z; z++)
            {
                for (int y = 0; y < min.y; y++)
                {
                    for (int x = 0; x < min.x; x++)
                    {
                        int newIndex = x + newDimensions.x * (y + newDimensions.y * z);
                        newNodeMatrix[newIndex] = TryGetNode(x, y, z);
                    }
                }
            }

            Dimensions = newDimensions;
            _nodeMatrix = newNodeMatrix;
        }

        public Vector3 CellToWorldPosition(Vector3Int cell)
        {
            return transform.TransformPoint(cell + cellOffset);
        }

        public int TryGetNodeIndex(Vector3Int cell)
        {
            return TryGetNodeIndex(cell.x, cell.y, cell.z);
        }
        public int TryGetNodeIndex(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= Dimensions.x || y >= Dimensions.y || z >= Dimensions.z) return -1;

            return x + Dimensions.x * (y + Dimensions.y * z);
        }

        public Node TryGetNode(Vector3Int cell)
        {
            return TryGetNode(cell.x, cell.y, cell.z);
        }
        public Node TryGetNode(int x, int y, int z)
        {
            int index = TryGetNodeIndex(x, y, z);
            if (index == -1 || index >= _nodeMatrix.Count) return null;

            return _nodeMatrix[index];
        }

        public bool TrySetNode(Vector3Int cell, Node node)
        {
            return TrySetNode(cell.x, cell.y, cell.z, node);
        }
        public bool TrySetNode(int x, int y, int z, Node node)
        {
            int index = TryGetNodeIndex(x, y, z);
            if (index == -1 || index >= _nodeMatrix.Count) return false;

            _nodeMatrix[index] = node;
            return true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube((Vector3)Dimensions / 2f, Dimensions);

            for (int x = 0; x < Dimensions.x; x++)
            {
                for (int y = 0; y < Dimensions.y; y++)
                {
                    for (int z = 0; z < Dimensions.z; z++)
                    {
                        if (HiddenCells.Contains(new Vector3Int(x, y, z))) continue;

                        Node node = TryGetNode(x, y, z);
                        if (node != null)
                        {
                            Gizmos.color = Color.green;
                            Vector3Int cell = new(x, y, z);
                            Gizmos.DrawWireCube(cell + cellOffset, transform.localScale);
                        }
                    }
                }
            }
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            _serializableHiddenCells = new List<Vector3Int>(HiddenCells);
#endif
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            HiddenCells = new HashSet<Vector3Int>(_serializableHiddenCells);
#endif
        }
    }
}
