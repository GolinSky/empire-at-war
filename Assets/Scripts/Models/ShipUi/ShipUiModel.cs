using System;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using UnityEngine;

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
        
        public Sprite ShipIcon { get; set; }

        public void UpdateSelection(SelectionType selectionType) => OnSelectionChanged?.Invoke(selectionType);

    }
}