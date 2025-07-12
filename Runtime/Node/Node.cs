using UnityEngine;

namespace GameplayGrid
{
    public class Node
    {
        public NodeProperties   Properties;
        public Grid             Grid        { get; private set; }
        public Vector3Int       Coordinates { get; private set; }

        public Node(NodeProperties properties, Vector3Int coordinates, Grid grid)
        {
            Properties = properties;
            Coordinates = coordinates;
            Grid = grid;
        }

        public Vector3 GetWorldPosition()
        {
            return Grid.transform.TransformPoint(Coordinates);
        }

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
    }
}
