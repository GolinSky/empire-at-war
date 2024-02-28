using System;
using EmpireAtWar.Models.Factions;
using Utilities.ScriptUtils.EditorSerialization;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.ShipUi
{
    public interface IShipUiModelObserver:IModelObserver
    {
        event Action<SelectionType> OnSelectionChanged;

        Sprite ShipIcon { get; }
        ShipInfoUi ShipInfoUi { get; }
    }
    
    [CreateAssetMenu(fileName = "ShipUiModel", menuName = "Model/ShipUiModel")]
    public class ShipUiModel : Model, IShipUiModelObserver
    {
        public event Action<SelectionType> OnSelectionChanged;

        [SerializeField] private DictionaryWrapper<ShipType, ShipInfoUi> shipUiWrapper;
        
        public Sprite ShipIcon { get; set; }
        public ShipInfoUi ShipInfoUi { get; set; }

        public void UpdateSelection(SelectionType selectionType) => OnSelectionChanged?.Invoke(selectionType);

        public ShipInfoUi GetShipInfoUi(ShipType shipType)
        {
            return shipUiWrapper.Dictionary[shipType];
        }
    }
}