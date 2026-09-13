using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Factions
{
    public interface IPlayerFactionModelObserver : IModelObserver
    {
        event Action<IReadOnlyList<ProductionQueueSnapshot>> OnProductionChanged;
        event Action<int> OnLevelUpgraded;
        event Action<SelectionType> OnSelectionTypeChanged;

        SelectionType SelectionType { get; }
        FactionType FactionType { get; }
        FactionData GetCurrentLevelFactionData();
        int CurrentLevel { get; }
        IReadOnlyList<ProductionQueueSnapshot> GetProductionQueueSnapshots();
    }

    public class PlayerFactionModel : PureModel, IPlayerFactionModelObserver
    {
        private const int MAX_ACTIVE_PIPELINES = 7;

        public event Action<IReadOnlyList<ProductionQueueSnapshot>> OnProductionChanged;
        public event Action<UnitRequest> OnUnitCompleted;
        public event Action<int> OnLevelUpgraded;
        public event Action<SelectionType> OnSelectionTypeChanged;

        private readonly PlayerFactionData _data;
        private readonly Dictionary<string, Queue<ProductionQueueItem>> _productionQueues = new();

        private SelectionType _selectionType;
        private int _currentLevel = 1;

        public PlayerFactionModel(
            PlayerFactionData data,
            FactionType factionType)
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
            if (unitRequest == null)
            {
                throw new ArgumentNullException(nameof(unitRequest));
            }

            if (!_productionQueues.TryGetValue(unitRequest.Id, out Queue<ProductionQueueItem> queue))
            {
                return unitRequest.FactionData.MaxCount > 0 &&
                    _productionQueues.Count < MAX_ACTIVE_PIPELINES;
            }

            return queue.Count < unitRequest.FactionData.MaxCount;
        }

        public void QueueUnit(UnitRequest unitRequest)
        {
            if (unitRequest == null)
            {
                throw new ArgumentNullException(nameof(unitRequest));
            }

            if (!CanQueueUnit(unitRequest))
            {
                throw new InvalidOperationException(
                    "The unit request exceeds the production queue capacity.");
            }

            if (!_productionQueues.TryGetValue(unitRequest.Id, out Queue<ProductionQueueItem> queue))
            {
                queue = new Queue<ProductionQueueItem>();
                _productionQueues.Add(unitRequest.Id, queue);
            }

            queue.Enqueue(new ProductionQueueItem(unitRequest));
            NotifyProductionChanged();
        }

        public bool TryCancelCurrentUnit(string id, out UnitRequest unitRequest)
        {
            if (!_productionQueues.TryGetValue(id, out Queue<ProductionQueueItem> queue))
            {
                unitRequest = null;
                return false;
            }

            unitRequest = queue.Dequeue().UnitRequest;
            if (queue.Count == 0)
            {
                _productionQueues.Remove(id);
            }

            NotifyProductionChanged();
            return true;
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (_productionQueues.Count == 0)
            {
                return;
            }

            List<string> pipelineIds = new List<string>(_productionQueues.Keys);
            foreach (string pipelineId in pipelineIds)
            {
                AdvancePipeline(pipelineId, deltaTime);
            }

            NotifyProductionChanged();
        }

        public IReadOnlyList<ProductionQueueSnapshot> GetProductionQueueSnapshots()
        {
            List<ProductionQueueSnapshot> snapshots =
                new List<ProductionQueueSnapshot>(_productionQueues.Count);
            foreach (Queue<ProductionQueueItem> queue in _productionQueues.Values)
            {
                ProductionQueueItem activeItem = queue.Peek();
                snapshots.Add(new ProductionQueueSnapshot(
                    activeItem.UnitRequest,
                    queue.Count,
                    activeItem.RemainingBuildTime));
            }

            return snapshots;
        }

        private void AdvancePipeline(string pipelineId, float deltaTime)
        {
            float remainingDeltaTime = deltaTime;
            while (_productionQueues.TryGetValue(
                       pipelineId,
                       out Queue<ProductionQueueItem> queue))
            {
                ProductionQueueItem activeItem = queue.Peek();
                if (activeItem.RemainingBuildTime > remainingDeltaTime)
                {
                    activeItem.Advance(remainingDeltaTime);
                    return;
                }

                remainingDeltaTime -= activeItem.RemainingBuildTime;
                UnitRequest completedUnit = queue.Dequeue().UnitRequest;
                if (queue.Count == 0)
                {
                    _productionQueues.Remove(pipelineId);
                }

                OnUnitCompleted?.Invoke(completedUnit);
                if (remainingDeltaTime <= 0f)
                {
                    return;
                }
            }
        }

        private void NotifyProductionChanged()
        {
            OnProductionChanged?.Invoke(GetProductionQueueSnapshots());
        }
    }
}
