using System;
using EmpireAtWar.Models.Factions;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.Serialization;

namespace EmpireAtWar.Models.ShipUi
{
    public interface IShipUiModelObserver:IModelObserver
    {
        event Action<SelectionType> OnSelectionChanged;
        Sprite ShipIcon { get; }
    }
    
    [CreateAssetMenu(fileName = "ShipUiModel", menuName = "Model/ShipUiModel")]
    public class ShipUiModel : Model, IShipUiModelObserver
    {
        public event Action<SelectionType> OnSelectionChanged;

        [FormerlySerializedAs("shipUiWrapper")] [SerializeField] private DictionaryWrapper<ShipType, Sprite> shipIconWrapper;
        public Sprite ShipIcon { get; set; }


        public void UpdateSelection(SelectionType selectionType) => OnSelectionChanged?.Invoke(selectionType);

        public Sprite GetShipIcon(ShipType shipType)
        {
            return shipIconWrapper.Dictionary[shipType];
        }
    }
}