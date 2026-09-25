using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public class EnemyFactionModel : IModel
    {
        private readonly FactionsData _factionsData;

        public EnemyFactionModel(FactionsData factionsData, FactionType factionType)
        {
            _factionsData = factionsData;
            FactionType = factionType;
        }

        public FactionType FactionType { get; }

        public Dictionary<ShipType, FactionData> ShipFactionData => _factionsData.GetShipFactionData(FactionType);
        public Dictionary<MiningFacilityType, FactionData> MiningFactions => _factionsData.MiningFactionsData;
        public Dictionary<DefendPlatformType, FactionData> DefendPlatforms => _factionsData.DefendPlatformDictionary;

        public int CurrentLevel { get; set; } = 1;

        public FactionData GetCurrentLevelFactionData()
        {
            return _factionsData.GetLevelFactionData(CurrentLevel);
        }
    }
}
