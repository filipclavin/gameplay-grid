using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameplayGrid
{
    public class Grid3D : MonoBehaviour
    {
        public List<List<List<Node>>> Nodes { get; private set; }

        public void Resize(int x, int y, int z)
        {
            Assert.IsTrue(x > 0 && y > 0 && z > 0, "Grid dimensions must be greater than zero.");

            if (x < Nodes.Count)
            {
                Nodes.RemoveRange(x, Nodes.Count - x);
            }

            for (int i = 0; i < x; i++)
            {
                if (i >= Nodes.Count)
                {
                    Nodes.Add(new List<List<Node>>());
                }

                if (y < Nodes[i].Count)
                {
                    Nodes[i].RemoveRange(y, Nodes[i].Count - y);
                    break;
                }

                for (int j = 0; j < y; j++)
                {
                    if (j >= Nodes[i].Count)
                    {
                        Nodes[i].Add(new List<Node>());
                    }

                    if (z < Nodes[i][j].Count)
                    {
                        Nodes[i][j].RemoveRange(z, Nodes[i][j].Count - z);
                        continue;
                    }

                    for (int k = 0; k < z; k++)
                    {
                        if (k >= Nodes[i][j].Count)
                        {
                            Nodes[i][j].Add(null);
                        }
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (Nodes == null || Nodes.Count == 0)
            {
                Nodes = new() { new() { new() { null } } };
            }
        }
    }
}
