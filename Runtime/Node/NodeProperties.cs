using UnityEngine;

namespace GameplayGrid
{
    [CreateAssetMenu(fileName = "NodeProperties", menuName = "Scriptable Objects/NodeProperties")]
    public class NodeProperties : ScriptableObject
    {
        public float EnterCost  = 0f;
        public float ExitCost   = 0f;
    }
}
