using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Mvc;
using EmpireAtWar.Views.Factions;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [CreateAssetMenu(fileName = nameof(PlayerFactionData), menuName = "Data/Player Faction Data")]
    public class PlayerFactionData : Data
    {
        [SerializeField] private FactionsModel factionsModel;

        [field: SerializeField] public FactionUnitUi FactionUnit { get; private set; }

        public IEnumerable<KeyValuePair<ShipType, FactionData>> GetShipFactionData(FactionType factionType)
        {
            return factionsModel.GetShipFactionData(factionType);
        }

        public IEnumerable<KeyValuePair<MiningFacilityType, FactionData>> GetMiningFactionData()
        {
            return factionsModel.MiningFactionsData;
        }

        public IEnumerable<KeyValuePair<DefendPlatformType, FactionData>> GetDefendPlatformData()
        {
            return factionsModel.DefendPlatformDictionary;
        }

        public FactionData GetLevelFactionData(int level)
        {
            return factionsModel.GetLevelFactionData(level);
        }
    }
}
