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
        private readonly ISpawnBlockerService _blockers;
        private readonly IShipSpawnClearance _clearance;
        private readonly IMapModelObserver _mapModel;

        public ReinforcementSpawnRule(
            IVisionService vision,
            ISpawnBlockerService blockers,
            IShipSpawnClearance clearance,
            IMapModelObserver mapModel)
        {
            _vision = vision;
            _blockers = blockers;
            _clearance = clearance;
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
            IsOpen(owner, position) && _clearance.IsClear(owner, shipType, position);
    }
}
