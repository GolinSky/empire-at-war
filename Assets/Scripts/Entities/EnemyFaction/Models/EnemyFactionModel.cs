using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public class EnemyFactionModel : IModel, IFactionLevelObserver
    {
        public const int MINIMUM_ASSAULT_CAPITALS = 3;

        private readonly StationLevelData _stationLevels;
        private readonly FactionRoster _roster;

        private int _currentLevel = 1;

        public event Action<int> OnLevelUpgraded;
        public event Action<int> LevelUpgradeCompleted;

        public int AdditionalStationsBuilt { get; set; }
        public bool HasAssaultSurplus { get; set; }
        public bool HasAssaultReinforcementPlan { get; set; }
        /// <summary>The chosen ship is affordable but does not fit the free unit capacity. Set by production.</summary>
        public bool IsAtUnitCap { get; set; }
        public int HeldShips { get; set; }
        /// <summary>Battle time of the last assault capital refunded for want of an escort body (W56); set by the controller.</summary>
        public float LastCapitalRefundAt { get; set; } = float.NegativeInfinity;

        /// <summary>The main station has no further level to buy.</summary>
        public bool IsAtTopLevel => GetCurrentLevelFactionData() == null;

        public FactionData SecondStationData => _stationLevels.GetSecondStationFactionData(_roster.FactionType);

        public bool IsAssaultCapital(ShipType type, ShipClass shipClass) =>
            (shipClass is ShipClass.Capital or ShipClass.BattleCruiser or ShipClass.Dreadnought) &&
            ShipFactionData.TryGetValue(type, out FactionData data) && data.AvailableLevel >= 4;

        public void CompleteLevelUpgrade()
        {
            CurrentLevel++;
            LevelUpgradeCompleted?.Invoke(CurrentLevel);
        }

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

        /// <summary>Cost of upgrading the second station from <paramref name="level"/>; null at max level.</summary>
        public FactionData GetSecondStationLevelFactionData(int level) =>
            _stationLevels.GetSecondStationLevelFactionData(level);
    }
}
