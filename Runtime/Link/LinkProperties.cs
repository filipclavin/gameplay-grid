using UnityEngine;

namespace GameplayGrid
{
    [CreateAssetMenu(fileName = "LinkProperties", menuName = "Scriptable Objects/LinkProperties")]
    public class LinkProperties : ScriptableObject
    {
        public float Cost = 1f;
    }
}
