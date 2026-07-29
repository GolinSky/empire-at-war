using System;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;

namespace EmpireAtWar.Components.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MapObstacle : MonoBehaviour, IMapObstacleContactSource
    {
        [SerializeField] private Collider _obstacleCollider;

        public RadarContact Contact
        {
            get
            {
                ValidateDependencies();
                Bounds bounds = _obstacleCollider.bounds;
                return new RadarContact(
                    bounds.center,
                    Mathf.Max(bounds.extents.x, bounds.extents.z),
                    false);
            }
        }

        private void Awake()
        {
            ValidateDependencies();
        }

        private void ValidateDependencies()
        {
            if (_obstacleCollider == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(MapObstacle)} requires a serialized obstacle collider.");
            }
        }
    }
}
