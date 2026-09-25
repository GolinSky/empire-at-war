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
        [SerializeField] private Collider _obstacleCollider;

        public Bounds WorldBounds => _obstacleCollider.bounds;

        public RadarContact Contact
        {
            get
            {
                Bounds bounds = _obstacleCollider.bounds;
                return new RadarContact(
                    bounds.center,
                    Mathf.Max(bounds.extents.x, bounds.extents.z),
                    false);
            }
        }

    }
}
