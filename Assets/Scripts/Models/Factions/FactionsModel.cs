using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
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

        private Dictionary<FactionType, FactionDataWrapper> FactionData => factionDataWrapper.Dictionary;
        public Dictionary<ShipType, FactionData> GetFactionData(FactionType factionType)
        {
            return FactionData[factionType].Dictionary;
        }
    }
}