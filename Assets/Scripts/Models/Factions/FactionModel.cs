using System;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Factions;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Factions
{
    public interface IFactionModelObserver : IModelObserver
    {
        event Action<SelectionType> OnSelectionTypeChanged;

        FactionUnitUi ShipUnit { get; }
        SelectionType SelectionType { get; }
    }

    [CreateAssetMenu(fileName = "FactionModel", menuName = "Model/FactionModel")]
    public class FactionModel : FactionModel<RepublicShipType>, IFactionModelObserver
    {
        public event Action<SelectionType> OnSelectionTypeChanged;
        
        [field: SerializeField] public FactionUnitUi ShipUnit { get; private set; }

        private SelectionType selectionType;
        
        public SelectionType SelectionType
        {
            get => selectionType;
            set
            {
                selectionType = value;
                OnSelectionTypeChanged?.Invoke(selectionType);

            }
        }
    }
}