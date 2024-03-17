using System.Collections.Generic;
using EmpireAtWar.Models.MiningFacility;
using Utilities.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    [CreateAssetMenu(fileName = "FactionsModel", menuName = "Model/FactionsModel")]
    public class FactionsModel : Model
    {
        [SerializeField] private DictionaryWrapper<FactionType, FactionDataWrapper> factionDataWrapper;
        [SerializeField] private FactionData[] levelFactionsData;
        [SerializeField] private DictionaryWrapper<MiningFacilityType, FactionData> miningFactionsData;
        [field: SerializeField] public int MaxLevel { get; private set; }
        
        private Dictionary<FactionType, FactionDataWrapper> FactionData => factionDataWrapper.Dictionary;

        public Dictionary<MiningFacilityType, FactionData> MiningFactionsData => miningFactionsData.Dictionary;

        public Dictionary<ShipType, FactionData> GetFactionData(FactionType factionType)
        {
            return FactionData[factionType].Dictionary;
        }

        // public FactionData GetMiningFactionData(MiningFacilityType miningFacilityType)
        // {
        //     return miningFactionsData.Dictionary[miningFacilityType];
        // }

        public FactionData GetLevelFactionData(int level)
        {
            if (level > MaxLevel) return null;
            
            return levelFactionsData[level-1];
        }
    }
}