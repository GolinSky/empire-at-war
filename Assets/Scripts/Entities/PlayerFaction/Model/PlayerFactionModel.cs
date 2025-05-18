using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Factions;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.Factions
{
    public interface IPlayerFactionModelObserver : IModelObserver
    {
        event Action<UnitRequest> OnUnitBuild;
        event Action<int> OnLevelUpgraded;
        event Action<SelectionType> OnSelectionTypeChanged;

        FactionUnitUi ShipUnit { get; }
        SelectionType SelectionType { get; }
        Dictionary<ShipType, FactionData> ShipFactionData { get; }
        Dictionary<MiningFacilityType, FactionData> MiningFactions { get; }
        Dictionary<DefendPlatformType, FactionData> DefendPlatforms { get; }
        FactionData GetCurrentLevelFactionData();
        int CurrentLevel { get; }
    }

    [CreateAssetMenu(fileName = "FactionModel", menuName = "Model/FactionModel")]
    public class PlayerFactionModel : Model, IPlayerFactionModelObserver
    {
        public event Action<UnitRequest> OnUnitBuild;
        public event Action<int> OnLevelUpgraded;
        public event Action<SelectionType> OnSelectionTypeChanged;
        
        [SerializeField] private FactionsModel factionsModel;
        [field: SerializeField] public FactionUnitUi ShipUnit { get; private set; }

        private SelectionType _selectionType;
        private int _currentLevel = 1;
        
        public SelectionType SelectionType
        {
            get => _selectionType;
            set
            {
                _selectionType = value;
                OnSelectionTypeChanged?.Invoke(_selectionType);
            }
        }

        [Inject(Id = PlayerType.Player)] 
        public FactionType FactionType { get; }
        
        public UnitRequest UnitToBuild
        {
            set => OnUnitBuild?.Invoke(value);
        }

        public Dictionary<MiningFacilityType, FactionData> MiningFactions => factionsModel.MiningFactionsData;
        public Dictionary<DefendPlatformType, FactionData> DefendPlatforms => factionsModel.DefendPlatformDictionary;
        public Dictionary<ShipType, FactionData> ShipFactionData => factionsModel.GetShipFactionData(FactionType);
        
        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                _currentLevel = value;
                OnLevelUpgraded?.Invoke(_currentLevel);
            }
        }

        public FactionData GetCurrentLevelFactionData()
        {
            return factionsModel.GetLevelFactionData(CurrentLevel);
        }
    }
}