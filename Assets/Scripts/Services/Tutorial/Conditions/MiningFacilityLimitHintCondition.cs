using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Met when every mining facility kind unlocked at the current station level has reached its build limit.
    /// Queued, waiting and placed facilities all count, as for the build button.
    /// </summary>
    public sealed class MiningFacilityLimitHintCondition : IPolledHintCondition
    {
        private readonly IPlayerFactionModelObserver _faction;
        private readonly List<MiningFacilityUnitRequest> _requests = new List<MiningFacilityUnitRequest>();

        public bool IsMet { get; private set; }

        public MiningFacilityLimitHintCondition(FactionRoster roster, IPlayerFactionModelObserver faction)
        {
            _faction = faction;
            foreach (KeyValuePair<MiningFacilityType, FactionData> facility in roster.MiningFacilities)
                _requests.Add(new MiningFacilityUnitRequest(facility.Value, facility.Key));
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            bool anyUnlocked = false;
            foreach (MiningFacilityUnitRequest request in _requests)
            {
                if (request.FactionData.AvailableLevel > _faction.CurrentLevel) continue;
                anyUnlocked = true;
                if (!_faction.IsUnitLimitReached(request)) return;
            }
            IsMet = anyUnlocked;
        }

        public void Dispose()
        {
        }
    }
}
