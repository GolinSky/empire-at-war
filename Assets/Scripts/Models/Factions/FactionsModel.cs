using System.Collections.Generic;
using Utilities.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EmpireAtWar.Models.Factions
{
    [CreateAssetMenu(fileName = "FactionsModel", menuName = "Model/FactionsModel")]
    public class FactionsModel : Model
    {
        [field: SerializeField]
        public DictionaryWrapper<ShipType, AssetReferenceGameObject> ShipViewReference { get; private set; }
        
        [SerializeField] private DictionaryWrapper<FactionType, FactionDataWrapper> factionDataWrapper;
        [SerializeField] private FactionData[] levelFactionsData;
        [field: SerializeField] public int MaxLevel { get; private set; }
        
        private Dictionary<FactionType, FactionDataWrapper> FactionData => factionDataWrapper.Dictionary;
        public Dictionary<ShipType, FactionData> GetFactionData(FactionType factionType)
        {
            return FactionData[factionType].Dictionary;
        }

        public FactionData GetLevelFactionData(int level)
        {
            if (level > MaxLevel) return null;
            
            return levelFactionsData[level-1];
        }
        
    }
}