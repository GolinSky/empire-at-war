using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Models.Factions
{
    public interface IPlayerFactionModelObserver : IModelObserver
    {
        event Action<UnitRequest> OnUnitBuild;
        event Action<int> OnLevelUpgraded;
        event Action<SelectionType> OnSelectionTypeChanged;

        SelectionType SelectionType { get; }
        FactionType FactionType { get; }
        FactionData GetCurrentLevelFactionData();
        int CurrentLevel { get; }
    }

    public class PlayerFactionModel : PureModel, IPlayerFactionModelObserver
    {
        public event Action<UnitRequest> OnUnitBuild;
        public event Action<int> OnLevelUpgraded;
        public event Action<SelectionType> OnSelectionTypeChanged;

        private readonly PlayerFactionData _data;
        private readonly List<UnitRequest> _buildingUnits = new();

        private SelectionType _selectionType;
        private int _currentLevel = 1;

        public PlayerFactionModel(
            PlayerFactionData data,
            [Inject(Id = PlayerType.Player)] FactionType factionType)
        {
            _data = data;
            FactionType = factionType;
        }
        
        public SelectionType SelectionType
        {
            get => _selectionType;
            set
            {
                _selectionType = value;
                OnSelectionTypeChanged?.Invoke(_selectionType);
            }
        }

        public FactionType FactionType { get; }
        
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
            return _data.GetLevelFactionData(CurrentLevel);
        }

        public bool CanQueueUnit(UnitRequest unitRequest)
        {
            int queuedCount = 0;
            foreach (UnitRequest request in _buildingUnits)
            {
                if (request.Id == unitRequest.Id)
                {
                    queuedCount++;
                }
            }

            return queuedCount < unitRequest.FactionData.MaxCount;
        }

        public void QueueUnit(UnitRequest unitRequest)
        {
            _buildingUnits.Add(unitRequest);
            OnUnitBuild?.Invoke(unitRequest);
        }

        public void CompleteUnit(UnitRequest unitRequest)
        {
            _buildingUnits.Remove(unitRequest);
        }
    }
}
