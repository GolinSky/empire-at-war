using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.SpaceStation
{
    /// <summary>Hangar data of one faction's space station.</summary>
    public sealed class StationHangarData : IHangarData
    {
        private readonly SpaceStationData _data;

        public IReadOnlyList<HangarBay> HangarBays { get; }
        public float HangarInitialDelay => _data.HangarInitialDelay;
        public float HangarLaunchInterval => _data.HangarLaunchInterval;

        public StationHangarData(SpaceStationData data, FactionType factionType)
        {
            _data = data;
            HangarBays = new[] { data.GetHangarBay(factionType) };
        }
    }
}
