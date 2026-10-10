using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;

namespace EmpireAtWar.Components.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MapObstacle : MonoBehaviour, IMapObstacleContactSource,
        IMiniMapObstacleSource
    {
        [SerializeField] private Collider obstacleCollider;

        public Bounds WorldBounds => obstacleCollider.bounds;

        public RadarContact Contact => RadarContact.FromBounds(obstacleCollider.bounds, false);

    }
}
