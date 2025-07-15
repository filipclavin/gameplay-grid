using UnityEngine;

namespace GameplayGrid
{
    [CreateAssetMenu(fileName = "LinkFactory", menuName = "Scriptable Objects/LinkFactory")]
    public class LinkFactory : ScriptableObject
    {
        public virtual Link CreateLink(Node fromNode, Node toNode)
        {
            return new Link(this, fromNode, toNode);
        }
    }
}
