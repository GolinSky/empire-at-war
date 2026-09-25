using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Factions
{
    public interface IFactionResearchModelObserver : IModelObserver
    {
        event Action<ResearchType> OnResearchCompleted;

        IReadOnlyCollection<ResearchType> ResearchTypes { get; }
        float IncomeMultiplier { get; }
        bool TryGetNextTier(ResearchType researchType, out ResearchTierData tier);
        float GetMultiplier(ResearchStat stat, ShipClass shipClass);
    }
}
