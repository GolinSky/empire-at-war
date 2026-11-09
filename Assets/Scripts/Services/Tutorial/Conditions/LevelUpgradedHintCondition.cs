using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class LevelUpgradedHintCondition : IHintCondition
    {
        private readonly IFactionLevelObserver _level;

        public bool IsMet { get; private set; }

        public LevelUpgradedHintCondition(IFactionLevelObserver level)
        {
            _level = level;
            _level.LevelUpgradeCompleted += HandleLevelUpgraded;
        }

        private void HandleLevelUpgraded(int level) => IsMet = true;

        public void Dispose() => _level.LevelUpgradeCompleted -= HandleLevelUpgraded;
    }
}
