using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class ReinforcementSpawnRule : IReinforcementSpawnRule
    {
        private readonly IVisionService _vision;
        private readonly IBattleAreaService _areas;
        private readonly IShipSpawnClearance _shipClearance;
        private readonly IShipSpawnReachability _shipReachability;
        private readonly IStructureSpawnClearance _structureClearance;
        private readonly IMapModelObserver _mapModel;
        private readonly IHazardService _hazards;

        public ReinforcementSpawnRule(
            IVisionService vision,
            IBattleAreaService areas,
            IShipSpawnClearance shipClearance,
            IShipSpawnReachability shipReachability,
            IStructureSpawnClearance structureClearance,
            IMapModelObserver mapModel,
            IHazardService hazards)
        {
            _vision = vision;
            _areas = areas;
            _shipClearance = shipClearance;
            _shipReachability = shipReachability;
            _structureClearance = structureClearance;
            _mapModel = mapModel;
            _hazards = hazards;
        }

        public bool IsOpen(PlayerId team, Vector3 position)
        {
            Vector2 min = _mapModel.SizeRange.Min;
            Vector2 max = _mapModel.SizeRange.Max;
            return position.x >= min.x && position.x <= max.x &&
                   position.z >= min.y && position.z <= max.y &&
                   _vision.IsVisible(team, position) &&
                   !_areas.IsBlocked(team, position) &&
                   !_hazards.BlocksReinforcement(position);
        }

        public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position) =>
            IsOpen(owner, position) && _shipClearance.IsClear(owner, shipType, position) &&
            _shipReachability.IsReachable(owner, shipType, position);

        public bool CanSpawnStructure(PlayerId owner, Vector3 position)
        {
            float radius = _structureClearance.Radius;
            return IsOpen(owner, position) &&
                   !_areas.IsStructureExcluded(position, radius) &&
                   _structureClearance.IsClear(position);
        }
    }
}
