using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Factions;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Factions
{
    public interface IPlayerFactionModelObserver : IModelObserver
    {
        event Action<SelectionType> OnSelectionTypeChanged;

        FactionUnitUi ShipUnit { get; }
        SelectionType SelectionType { get; }
        Dictionary<ShipType, FactionData> FactionData { get; }
    }

    [CreateAssetMenu(fileName = "FactionModel", menuName = "Model/FactionModel")]
    public class PlayerFactionModel : Model, IPlayerFactionModelObserver
    {
        public event Action<SelectionType> OnSelectionTypeChanged;
        
        [SerializeField] private FactionsModel factionsModel;
        private SelectionType selectionType;

        [field: SerializeField] public FactionUnitUi ShipUnit { get; private set; }

        [Inject]
        public SkirmishGameData SkirmishGameData { get; }
        public SelectionType SelectionType
        {
            get => selectionType;
            set
            {
                selectionType = value;
                OnSelectionTypeChanged?.Invoke(selectionType);
            }
        }

        public Dictionary<ShipType, FactionData> FactionData => GetFactionData(SkirmishGameData.PlayerFactionType);

        public Dictionary<ShipType, FactionData> GetFactionData(FactionType factionType) => factionsModel.GetFactionData(factionType);
      
    }
}