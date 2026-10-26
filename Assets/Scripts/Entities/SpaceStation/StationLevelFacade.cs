using System;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.SpaceStation
{
    public sealed class StationLevelFacade : IStationLevelFacade
    {
        private readonly IFactionLevelObserver _level;

        public StationLevelFacade(IFactionLevelObserver level) => _level = level;

        public int CurrentLevel => _level.CurrentLevel;

        public event Action<int> LevelUpgraded
        {
            add => _level.LevelUpgradeCompleted += value;
            remove => _level.LevelUpgradeCompleted -= value;
        }
    }
}
