using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    public sealed class StationFootprintFacade : INavigationFootprintFacade, IStationHullFacade
    {
        private readonly BoxCollider _hullCollider;

        // Map-generation keep-out radius; navigation blocks only the hull (GetHullRadius).
        public float NavigationRadius { get; }
        public float HullBottom => _hullCollider.bounds.min.y;
        public float HullTop => _hullCollider.bounds.max.y;

        // Stations only yaw, so the box's planar half-diagonal bounds it at any facing.
        public float GetHullRadius(Vector3 center)
        {
            Vector3 extents = Vector3.Scale(_hullCollider.size, _hullCollider.transform.lossyScale) * 0.5f;
            Vector3 offset = _hullCollider.bounds.center - center;
            return new Vector2(offset.x, offset.z).magnitude + new Vector2(extents.x, extents.z).magnitude;
        }

        public StationFootprintFacade(MapGenerationSettings settings, FactionType factionType, BoxCollider hullCollider)
        {
            NavigationRadius = settings.GetStationRadius(factionType);
            _hullCollider = hullCollider;
        }
    }
}
