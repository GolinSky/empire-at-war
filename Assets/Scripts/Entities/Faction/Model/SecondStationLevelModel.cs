using System;

namespace EmpireAtWar.Models.Factions
{
    /// <summary>
    /// The player's second station: whether it stands and its own level, independent of the main station.
    /// A lost station keeps its level until the next one is built, so its death and wreck show the level it fell at.
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
        }

        /// <summary>A newly bought second station starts again at level 1.</summary>
        public void ResetForNewStation()
        {
            if (_currentLevel != 1) CurrentLevel = 1;
        }
    }
}
