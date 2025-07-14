using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameplayGrid
{
    public class Grid3D : MonoBehaviour
    {
        [field: SerializeField, Min(1)] public Vector3Int Dimensions { get; private set; } = new(1, 1, 1);

        public List<List<List<Node>>> NodeMatrix { get; private set; } = new() { new() { new() { null } } };

        public void SetDimensions(Vector3Int dimensions)
        {
            SetDimensions(dimensions.x, dimensions.y, dimensions.z);
        }

        public void SetDimensions(int x, int y, int z)
        {
            Assert.IsTrue(x > 0 && y > 0 && z > 0, "Grid dimensions must be greater than zero.");

            if (x < NodeMatrix.Count)
            {
                NodeMatrix.RemoveRange(x, NodeMatrix.Count - x);
            }

            for (int i = 0; i < x; i++)
            {
                if (i >= NodeMatrix.Count)
                {
                    NodeMatrix.Add(new List<List<Node>>());
                }

                if (y < NodeMatrix[i].Count)
                {
                    NodeMatrix[i].RemoveRange(y, NodeMatrix[i].Count - y);
                    break;
                }

                for (int j = 0; j < y; j++)
                {
                    if (j >= NodeMatrix[i].Count)
                    {
                        NodeMatrix[i].Add(new List<Node>());
                    }

                    if (z < NodeMatrix[i][j].Count)
                    {
                        NodeMatrix[i][j].RemoveRange(z, NodeMatrix[i][j].Count - z);
                        continue;
                    }

                    for (int k = 0; k < z; k++)
                    {
                        if (k >= NodeMatrix[i][j].Count)
                        {
                            NodeMatrix[i][j].Add(null);
                        }
                    }
                }
            }
        }

        public Vector3 CoordinatesToWorldPosition(Vector3Int coordinates)
        {
            return transform.TransformPoint(coordinates + new Vector3(.5f, .5f, .5f));
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube((Vector3)Dimensions / 2f, Dimensions);

            foreach (var x in NodeMatrix)
            {
                foreach (var y in x)
                {
                    foreach (var node in y)
                    {
                        if (node != null)
                        {
                            Gizmos.color = node.IsEnabled ? Color.green : Color.red;
                            Gizmos.DrawWireCube(node.Cell, transform.localScale);
                        }
                    }
                }
            }
        }
    }
}
