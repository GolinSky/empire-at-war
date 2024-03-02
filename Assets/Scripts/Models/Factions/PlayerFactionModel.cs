using System;
using System.Collections.Generic;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Factions;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Factions
{
    public interface IPlayerFactionModelObserver : IModelObserver
    {
        event Action<ShipType> OnShipBuild;

        event Action<int> OnLevelUpgraded;
        event Action<SelectionType> OnSelectionTypeChanged;

        FactionUnitUi ShipUnit { get; }
        SelectionType SelectionType { get; }
        Dictionary<ShipType, FactionData> FactionData { get; }
        int CurrentLevel { get; }
    }

    [CreateAssetMenu(fileName = "FactionModel", menuName = "Model/FactionModel")]
    public class PlayerFactionModel : Model, IPlayerFactionModelObserver
    {
        public event Action<ShipType> OnShipBuild;
        public event Action<int> OnLevelUpgraded;
        public event Action<SelectionType> OnSelectionTypeChanged;
        
        [SerializeField] private FactionsModel factionsModel;
        [field: SerializeField] public FactionUnitUi ShipUnit { get; private set; }

        private SelectionType selectionType;
        private int currentLevel = 1;
        
        public SelectionType SelectionType
        {
            get => selectionType;
            set
            {
                selectionType = value;
                OnSelectionTypeChanged?.Invoke(selectionType);
            }
        }

        [Inject(Id = PlayerType.Player)] 
        public FactionType FactionType { get; }
        
        public ShipType ShipTypeToBuild
        {
            set => OnShipBuild?.Invoke(value);
        }
        
        public Dictionary<ShipType, FactionData> FactionData => GetFactionData(FactionType);

        public Dictionary<ShipType, FactionData> GetFactionData(FactionType factionType) => factionsModel.GetFactionData(factionType);

        public int CurrentLevel
        {
            get => currentLevel;
            set
            {
                currentLevel = value;
                OnLevelUpgraded?.Invoke(currentLevel);
            }
        }

    }
}