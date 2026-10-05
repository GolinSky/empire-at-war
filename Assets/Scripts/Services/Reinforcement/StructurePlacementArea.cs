using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;

namespace EmpireAtWar.Services.Reinforcement
{
    /// <summary>Structures follow the reinforcement spawn rule and stay outside relay rings and capture sites.</summary>
    public sealed class StructurePlacementArea
    {
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly IReinforcementZonesSystem _zones;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly PlayerSlot _owner;

        public StructurePlacementArea(
            IReinforcementSpawnRule spawnRule,
            IReinforcementZonesSystem zones,
            ICaptureSitesSystem captureSites,
            PlayerSlot owner)
        {
            _spawnRule = spawnRule;
            _zones = zones;
            _captureSites = captureSites;
            _owner = owner;
        }

        public bool Contains(Vector3 position)
        {
            return _spawnRule.IsOpen(_owner.Id, position) &&
                   !_zones.IsPositionInAnyZone(position) &&
                   !_captureSites.IsPositionInAnySite(position);
        }
    }
}
