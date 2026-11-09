using System.Collections.Generic;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a mining facility is unlocked at the current station level and affordable.</summary>
    public sealed class MiningFacilityAffordableHintCondition : IPolledHintCondition
    {
        private readonly FactionRoster _roster;
        private readonly IPlayerFactionModelObserver _faction;
        private readonly IEconomyModelObserver _economy;

        public bool IsMet { get; private set; }

        public MiningFacilityAffordableHintCondition(FactionRoster roster, IPlayerFactionModelObserver faction,
            IEconomyModelObserver economy)
        {
            _roster = roster;
            _faction = faction;
            _economy = economy;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            foreach (KeyValuePair<MiningFacilityType, FactionData> facility in _roster.MiningFacilities)
                if (facility.Value.AvailableLevel <= _faction.CurrentLevel && _economy.Money >= facility.Value.Price)
                {
                    IsMet = true;
                    return;
                }
        }

        public void Dispose()
        {
        }
    }
}
