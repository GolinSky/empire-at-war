using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public class EnemyFactionModel : IModel, IFactionLevelObserver
    {
        private readonly StationLevelData _stationLevels;
        private readonly FactionRoster _roster;

        private int _currentLevel = 1;

        public event Action<int> OnLevelUpgraded;

        public Dictionary<ShipType, FactionData> ShipFactionData => _roster.Ships;
        public Dictionary<SquadronType, FactionData> SquadronFactionData => _roster.Squadrons;
        public Dictionary<MiningFacilityType, FactionData> MiningFactions => _roster.MiningFacilities;
        public Dictionary<DefendPlatformType, FactionData> DefendPlatforms => _roster.DefendPlatforms;
        public Dictionary<SuperWeaponType, FactionData> SuperWeapons => _roster.SuperWeapons;

        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                _currentLevel = value;
                OnLevelUpgraded?.Invoke(_currentLevel);
            }
        }

        public EnemyFactionModel(StationLevelData stationLevels, FactionRoster roster)
        {
            _stationLevels = stationLevels;
            _roster = roster;
        }

        public FactionData GetCurrentLevelFactionData()
        {
            return _stationLevels.GetLevelFactionData(CurrentLevel);
        }
    }
}
