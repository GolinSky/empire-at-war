using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when any next research tier is unlocked at the current station level and affordable.</summary>
    public sealed class ResearchAffordableHintCondition : IPolledHintCondition
    {
        private readonly IFactionResearchModelObserver _research;
        private readonly IPlayerFactionModelObserver _faction;
        private readonly IEconomyModelObserver _economy;

        public bool IsMet { get; private set; }

        public ResearchAffordableHintCondition(IFactionResearchModelObserver research,
            IPlayerFactionModelObserver faction, IEconomyModelObserver economy)
        {
            _research = research;
            _faction = faction;
            _economy = economy;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            foreach (ResearchType type in _research.ResearchTypes)
                if (_research.TryGetNextTier(type, out ResearchTierData tier) &&
                    tier.FactionData.AvailableLevel <= _faction.CurrentLevel &&
                    _economy.Money >= tier.FactionData.Price)
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
