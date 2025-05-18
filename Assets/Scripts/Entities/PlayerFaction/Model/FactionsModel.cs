using System.Collections.Generic;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.DefendPlatform;
using Utilities.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [CreateAssetMenu(fileName = "FactionsModel", menuName = "Model/FactionsModel")]
    public class FactionsModel : Model
    {
        [SerializeField] private DictionaryWrapper<FactionType, FactionDataWrapper> factionDataWrapper;
        [SerializeField] private DictionaryWrapper<MiningFacilityType, FactionData> miningFactionsData;
        [SerializeField] private DictionaryWrapper<DefendPlatformType, FactionData> defendPlatformWrapper;

        [SerializeField] private FactionData[] levelFactionsData;
                
        [field: SerializeField] public int MaxLevel { get; private set; }
        

        public Dictionary<MiningFacilityType, FactionData> MiningFactionsData => miningFactionsData.Dictionary;

        public Dictionary<ShipType, FactionData> GetShipFactionData(FactionType factionType)
        {
            return factionDataWrapper.Dictionary[factionType].Dictionary;
        }

        public Dictionary<DefendPlatformType, FactionData> DefendPlatformDictionary => defendPlatformWrapper.Dictionary;

        public FactionData GetLevelFactionData(int level)
        {
            if (level >= MaxLevel) return null;
            
            return levelFactionsData[level-1];
        }
    }
}