using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the next station level exists and the player has the money for it.</summary>
    public sealed class LevelUpAffordableHintCondition : IPolledHintCondition
    {
        private readonly IPlayerFactionModelObserver _faction;
        private readonly IEconomyModelObserver _economy;

        public bool IsMet { get; private set; }

        public LevelUpAffordableHintCondition(IPlayerFactionModelObserver faction, IEconomyModelObserver economy)
        {
            _faction = faction;
            _economy = economy;
        }

        public void Poll(float deltaTime)
        {
            FactionData next = _faction.GetCurrentLevelFactionData();
            if (next != null && _economy.Money >= next.Price) IsMet = true;
        }

        public void Dispose()
        {
        }
    }
}
