using UnityEngine;

namespace EmpireAtWar.Components.Obstacles
{
    [DisallowMultipleComponent]
    public sealed class MapObstacle : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float avoidanceRadius = 12f;

        public float AvoidanceRadius => avoidanceRadius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
        }
    }
}
