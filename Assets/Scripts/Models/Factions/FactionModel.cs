using System;
using System.Collections.Generic;
using EmpireAtWar.ScriptUtils.EditorSerialization;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Factions;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Models.Factions
{
    public interface IFactionModelObserver : IModelObserver
    {
        event Action<SelectionType> OnSelectionTypeChanged;

        FactionUnitUi ShipUnit { get; }
        SelectionType SelectionType { get; }
        Dictionary<RepublicShipType, FactionData> FactionData { get; }
    }

    [CreateAssetMenu(fileName = "FactionModel", menuName = "Model/FactionModel")]
    public class FactionModel : FactionModel<RepublicShipType>, IFactionModelObserver
    {
        public event Action<SelectionType> OnSelectionTypeChanged;
        

        [SerializeField] private DictionaryWrapper<RepublicShipType, FactionData> factionDataWrapper;
        
        [field: SerializeField] public FactionUnitUi ShipUnit { get; private set; }
        public Dictionary<RepublicShipType, FactionData> FactionData => factionDataWrapper.Dictionary;
        
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