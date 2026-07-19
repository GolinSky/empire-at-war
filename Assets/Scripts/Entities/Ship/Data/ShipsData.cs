using System;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.Ship.Data
{
    [CreateAssetMenu(fileName = "ShipsData", menuName = "Data/ShipsData")]
    public class ShipsData : Mvc.Data
    {
        [SerializeField] private DictionaryWrapper<ShipType, AssetReferenceT<ShipData>> shipsData;
        
        public string GetShipDataPath(ShipType shipType)
        {
            return shipsData.Dictionary[shipType].AssetGUID;
        }
    }
}