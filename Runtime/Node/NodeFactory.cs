using UnityEngine;

namespace GameplayGrid
{
    [CreateAssetMenu(fileName = "NodeFactory", menuName = "Scriptable Objects/NodeFactory")]
    public class NodeFactory : ScriptableObject
    {
        public virtual Node CreateNode(Vector3Int coordinates, Grid3D grid)
        {
            return new Node(grid, coordinates);
        }
    }
}
