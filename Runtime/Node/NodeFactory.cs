using UnityEngine;

namespace GameplayGrid
{
    [CreateAssetMenu(fileName = "NodeFactory", menuName = "Scriptable Objects/NodeFactory")]
    public class NodeFactory : ScriptableObject
    {
        public virtual Node CreateNode(Grid3D grid, Vector3Int cell)
        {
            return new Node(this, grid, cell);
        }
    }
}
