using System;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>
    /// The player's second station: whether it stands and its own level, independent of the main station.
    /// A lost second station resets to level 1 for the next one.
    /// </summary>
    public sealed class SecondStationLevelModel : IFactionLevelObserver
    {
        private int _currentLevel = 1;
        private bool _isBuilt;

        public event Action<int> OnLevelUpgraded;
        public event Action<int> LevelUpgradeCompleted;
        public event Action<bool> BuiltChanged;

        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                _currentLevel = value;
                OnLevelUpgraded?.Invoke(_currentLevel);
            }
        }

        public bool IsBuilt
        {
            get => _isBuilt;
            set
            {
                if (_isBuilt == value) return;
                _isBuilt = value;
                BuiltChanged?.Invoke(_isBuilt);
            }
        }

        public void CompleteLevelUpgrade()
        {
            CurrentLevel++;
            LevelUpgradeCompleted?.Invoke(CurrentLevel);
        }

        public void HandleLost()
        {
            IsBuilt = false;
            CurrentLevel = 1;
        }
    }
}
