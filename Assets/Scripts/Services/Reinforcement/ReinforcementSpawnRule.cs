using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class ReinforcementSpawnRule : IReinforcementSpawnRule
    {
        private readonly IVisionService _vision;
        private readonly ISpawnBlockerService _blockers;
        private readonly IShipSpawnClearance _shipClearance;
        private readonly IStructureSpawnClearance _structureClearance;
        private readonly IReinforcementZonesSystem _relays;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly IMapModelObserver _mapModel;

        public ReinforcementSpawnRule(
            IVisionService vision,
            ISpawnBlockerService blockers,
            IShipSpawnClearance shipClearance,
            IStructureSpawnClearance structureClearance,
            IReinforcementZonesSystem relays,
            ICaptureSitesSystem captureSites,
            IMapModelObserver mapModel)
        {
            _vision = vision;
            _blockers = blockers;
            _shipClearance = shipClearance;
            _structureClearance = structureClearance;
            _relays = relays;
            _captureSites = captureSites;
            _mapModel = mapModel;
        }

        public bool IsOpen(PlayerId team, Vector3 position)
        {
            Vector2 min = _mapModel.SizeRange.Min;
            Vector2 max = _mapModel.SizeRange.Max;
            return position.x >= min.x && position.x <= max.x &&
                   position.z >= min.y && position.z <= max.y &&
                   _vision.IsVisible(team, position) &&
                   !_blockers.IsBlocked(team, position);
        }

        public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position) =>
            IsOpen(owner, position) && _shipClearance.IsClear(owner, shipType, position);

        public bool CanSpawnStructure(PlayerId owner, Vector3 position)
        {
            float radius = _structureClearance.Radius;
            return IsOpen(owner, position) &&
                   !_relays.IsPositionInAnyZone(position, radius) &&
                   !_captureSites.IsPositionInAnySite(position, radius) &&
                   _structureClearance.IsClear(position);
        }
    }
}
