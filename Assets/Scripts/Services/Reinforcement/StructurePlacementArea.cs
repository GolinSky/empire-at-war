using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using ViewComponents;

namespace EmpireAtWar.Services.Reinforcement
{
    /// <summary>Structures go on visible open space: outside reinforcement zones and capture sites.</summary>
    public sealed class StructurePlacementArea
    {
        private readonly IFogOfWarSystem _fogOfWarSystem;
        private readonly IReinforcementZonesSystem _zones;
        private readonly ICaptureSitesSystem _captureSites;

        public StructurePlacementArea(
            IFogOfWarSystem fogOfWarSystem,
            IReinforcementZonesSystem zones,
            ICaptureSitesSystem captureSites)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _zones = zones;
            _captureSites = captureSites;
        }

        public bool Contains(Vector3 position)
        {
            return !_fogOfWarSystem.IsHidden(position) &&
                   !_zones.IsPositionInAnyZone(position) &&
                   !_captureSites.IsPositionInAnySite(position);
        }
    }
}
