using NUnit.Framework;
using UnityEngine;

namespace GameplayGrid
{
    public class Link
    {
        public Node     FromNode    { get; private set; }
        public Node     ToNode      { get; private set; }
        public float    Cost        { get; private set; }

        public Link(Node fromNode, Node toNode, float cost = 1f)
        {
            Assert.IsNotNull(fromNode, "FromNode cannot be null.");
            Assert.IsNotNull(toNode, "ToNode cannot be null.");

            FromNode    = fromNode;
            ToNode      = toNode;
            Cost        = cost;
        }

        public virtual void OnUse(GameObject agent) {}
    }
}
