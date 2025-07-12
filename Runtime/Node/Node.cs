using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameplayGrid
{
    public class Node
    {
        public Grid3D       Grid            { get; private set; }
        public Vector3Int   Coordinates     { get; private set; }
        public float        EntryCost;
        public float        ExitCost;
        public bool         IsEnabled;
        
        public List<Link>   Links           { get; private set; } = new();

        public Node(Grid3D grid, Vector3Int coordinates, float entryCost = 0f, float exitCost = 0f, bool isEnabled = true)
        {
            Assert.IsNotNull(grid, "Grid cannot be null.");

            Grid        = grid;
            Coordinates = coordinates;
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
            return Grid.transform.TransformPoint(Coordinates);
        }

        public virtual void OnEnter(GameObject agent, Link fromLink) { }
        public virtual void OnExit(GameObject agent, Link toLink) { }
    }
}
