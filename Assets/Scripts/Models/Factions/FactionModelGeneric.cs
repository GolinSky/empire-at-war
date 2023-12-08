using EmpireAtWar.ScriptUtils.EditorSerialization;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EmpireAtWar.Models.Factions
{
    public class FactionModel<TShipType> : Model 
    {
        [field: SerializeField] public DictionaryWrapper<TShipType, AssetReferenceGameObject>  ShipViewReference { get; private set; }

    }
}