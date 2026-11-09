using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a next research tier (of one group, or any when null) is unlocked and affordable.</summary>
    public sealed class ResearchAffordableHintCondition : IPolledHintCondition
    {
        private readonly IFactionResearchModelObserver _research;
        private readonly IPlayerFactionModelObserver _faction;
        private readonly IEconomyModelObserver _economy;
        private readonly bool? _isEconomy;

        public bool IsMet { get; private set; }

        public ResearchAffordableHintCondition(IFactionResearchModelObserver research,
            IPlayerFactionModelObserver faction, IEconomyModelObserver economy, bool? isEconomy)
        {
            _isEconomy = isEconomy;
            _research = research;
            _faction = faction;
            _economy = economy;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            foreach (ResearchType type in _research.ResearchTypes)
                if ((!_isEconomy.HasValue || ResearchGroup.IsEconomy(type) == _isEconomy.Value) &&
                    _research.TryGetNextTier(type, out ResearchTierData tier) &&
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
