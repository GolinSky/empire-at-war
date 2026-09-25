using System.Collections.Generic;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [CreateAssetMenu(fileName = nameof(FactionsData), menuName = "Data/FactionsData")]
    public class FactionsData : Data
    {
        [SerializeField] private DictionaryWrapper<FactionType, FactionDataWrapper> factionDataWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, SquadronFactionDataWrapper> squadronFactionDataWrapper;
        [SerializeField] private DictionaryWrapper<MiningFacilityType, FactionData> miningFactionsData;
        [SerializeField] private DictionaryWrapper<DefendPlatformType, FactionData> defendPlatformWrapper;
        [SerializeField] private DictionaryWrapper<FactionType, FactionResearchWrapper> researchWrapper;
        [SerializeField] private DictionaryWrapper<SuperWeaponType, FactionData> superWeaponWrapper;

        [SerializeField] private FactionData[] levelFactionsData;
                
        [field: SerializeField] public int MaxLevel { get; private set; }
        

        public Dictionary<MiningFacilityType, FactionData> MiningFactionsData => miningFactionsData.Dictionary;

        public Dictionary<ShipType, FactionData> GetShipFactionData(FactionType factionType)
        {
            return factionDataWrapper.Dictionary[factionType].Dictionary;
        }

        public Dictionary<SquadronType, FactionData> GetSquadronFactionData(FactionType factionType)
        {
            return squadronFactionDataWrapper.Dictionary[factionType].Dictionary;
        }

        public FactionData GetSquadronFactionData(SquadronType squadronType)
        {
            foreach (SquadronFactionDataWrapper wrapper in squadronFactionDataWrapper.Dictionary.Values)
            {
                if (wrapper.Dictionary.TryGetValue(squadronType, out FactionData factionData))
                {
                    return factionData;
                }
            }

            throw new KeyNotFoundException($"No faction data for squadron {squadronType}.");
        }

        public Dictionary<DefendPlatformType, FactionData> DefendPlatformDictionary => defendPlatformWrapper.Dictionary;

        public Dictionary<SuperWeaponType, FactionData> SuperWeaponFactionData => superWeaponWrapper.Dictionary;

        public Dictionary<ResearchType, ResearchLineData> GetResearchLines(FactionType factionType)
        {
            return researchWrapper.Dictionary[factionType].Dictionary;
        }

        public FactionData GetLevelFactionData(int level)
        {
            if (level >= MaxLevel) return null;
            
            return levelFactionsData[level-1];
        }
    }
}