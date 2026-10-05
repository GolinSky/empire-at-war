using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public class EnemyFactionModel : IModel, IFactionLevelObserver
    {
        private readonly FactionsData _factionsData;

        private int _currentLevel = 1;

        public event Action<int> OnLevelUpgraded;

        public FactionType FactionType { get; }

        public Dictionary<ShipType, FactionData> ShipFactionData => _factionsData.GetShipFactionData(FactionType);
        public Dictionary<SquadronType, FactionData> SquadronFactionData =>
            _factionsData.GetSquadronFactionData(FactionType);
        public Dictionary<MiningFacilityType, FactionData> MiningFactions => _factionsData.MiningFactionsData;
        public Dictionary<DefendPlatformType, FactionData> DefendPlatforms => _factionsData.DefendPlatformDictionary;

        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                _currentLevel = value;
                OnLevelUpgraded?.Invoke(_currentLevel);
            }
        }

        public EnemyFactionModel(FactionsData factionsData, FactionType factionType)
        {
            _factionsData = factionsData;
            FactionType = factionType;
        }

        public FactionData GetCurrentLevelFactionData()
        {
            return _factionsData.GetLevelFactionData(CurrentLevel);
        }
    }
}
