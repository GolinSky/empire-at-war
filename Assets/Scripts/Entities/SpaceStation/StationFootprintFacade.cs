using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    public sealed class StationFootprintFacade : INavigationFootprintFacade, IStationHullFacade
    {
        private readonly BoxCollider _hullCollider;

        // The same radius the map registers as the station's navigation obstacle.
        public float NavigationRadius { get; }
        public float HullBottom => _hullCollider.bounds.min.y;
        public float HullTop => _hullCollider.bounds.max.y;

        public StationFootprintFacade(MapGenerationSettings settings, FactionType factionType, BoxCollider hullCollider)
        {
            NavigationRadius = settings.GetStationRadius(factionType);
            _hullCollider = hullCollider;
        }
    }
}
