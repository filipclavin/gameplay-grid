using NUnit.Framework;
using System;
using UnityEngine;

namespace GameplayGrid
{
    [Serializable]
    public class Link
    {
        [field: SerializeField]     public LinkFactory  LinkFactory { get; private set; }
        [field: SerializeReference] public Node         FromNode    { get; private set; }
        [field: SerializeReference] public Node         ToNode      { get; private set; }
        [field: SerializeField]     public float        Cost        { get; private set; }

        public Link(LinkFactory linkFactory, Node fromNode, Node toNode, float cost = 1f)
        {
            Assert.IsNotNull(linkFactory, "LinkFactory cannot be null.");
            Assert.IsNotNull(fromNode, "FromNode cannot be null.");
            Assert.IsNotNull(toNode, "ToNode cannot be null.");

            LinkFactory = linkFactory;
            FromNode    = fromNode;
            ToNode      = toNode;
            Cost        = cost;
        }

        public virtual void OnUse(GameObject agent) {}
    }
}
