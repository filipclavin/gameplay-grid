using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameplayGrid
{
    [Serializable]
    public class Node
    {
        [field: SerializeField] public NodeFactory  NodeFactory { get; private set; }
        [field: SerializeField] public Grid3D       Grid { get; private set; }
        [field: SerializeField] public Vector3Int   Cell { get; private set; }

        [SerializeField] public float   EntryCost;
        [SerializeField] public float   ExitCost;
        [SerializeField] public bool    IsEnabled;
        
        [field: SerializeField] public List<Link> Links { get; private set; } = new();

        public Node(NodeFactory nodeFactory, Grid3D grid, Vector3Int cell, float entryCost = 0f, float exitCost = 0f, bool isEnabled = true)
        {
            Assert.IsNotNull(nodeFactory, "NodeFactory cannot be null.");
            Assert.IsNotNull(grid, "Grid cannot be null.");

            NodeFactory = nodeFactory;
            Grid        = grid;
            Cell        = cell;
            EntryCost   = entryCost;
            ExitCost    = exitCost;
            IsEnabled   = isEnabled;
        }

        ~Node()
        {
            foreach (var link in Links)
            {
                if (link.FromNode == this)
                {
                    link.ToNode.Links.Remove(link);
                }
                else if (link.ToNode == this)
                {
                    link.FromNode.Links.Remove(link);
                }
            }
        }

        public Vector3 GetWorldPosition()
        {
            return Grid.CellToWorldPosition(Cell);
        }

        public virtual void OnEnter(GameObject agent, Link fromLink) { }
        public virtual void OnExit(GameObject agent, Link toLink) { }
    }
}
