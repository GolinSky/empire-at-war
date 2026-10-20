using EmpireAtWar.Components.Radar;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;

namespace EmpireAtWar.Components.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MapObstacle : MonoBehaviour, IMapObstacleContactSource,
        IObstacleBoundsSource
    {
        [SerializeField] private Collider obstacleCollider;

        public Bounds WorldBounds => obstacleCollider.bounds;

        public RadarContact Contact => RadarContact.FromBounds(obstacleCollider.bounds, false);

    }
}
