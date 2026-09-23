using EmpireAtWar.Models.Factions;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.Serialization;

namespace EmpireAtWar.Models.ShipUi
{
    [CreateAssetMenu(fileName = nameof(ShipUiData), menuName = "Data/ShipUiData")]
    public class ShipUiData : Data
    {
        [FormerlySerializedAs("shipUiWrapper")] [SerializeField] private DictionaryWrapper<ShipType, Sprite> shipIconWrapper;

        public Sprite GetShipIcon(ShipType shipType)
        {
            return shipIconWrapper.Dictionary[shipType];
        }
    }
}
