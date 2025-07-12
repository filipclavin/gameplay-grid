using UnityEngine;

namespace GameplayGrid
{
    public class Link
    {
        public LinkProperties Properties;

        public Node FromNode    { get; private set; }
        public Node ToNode      { get; private set; }

        public Link(LinkProperties properties, Node fromNode, Node toNode)
        {
            Properties = properties;
            FromNode = fromNode;
            ToNode = toNode;
        }

        protected virtual void OnEnter() { }
    }
}
