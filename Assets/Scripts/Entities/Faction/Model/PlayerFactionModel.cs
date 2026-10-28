using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Factions
{
    public interface IPlayerFactionModelObserver : IModelObserver, IFactionLevelObserver
    {
        event Action<IReadOnlyList<ProductionQueueSnapshot>> OnProductionChanged;

        event Action<SelectionType> OnSelectionTypeChanged;

        SelectionType SelectionType { get; }
        bool IsHeroReserved(ShipType shipType);
        bool IsUnitLimitReached(UnitRequest unitRequest);
        FactionType FactionType { get; }

        FactionData GetCurrentLevelFactionData();

        IReadOnlyList<ProductionQueueSnapshot> GetProductionQueueSnapshots();
    }

    public class PlayerFactionModel : Model, IPlayerFactionModelObserver
    {
        public const int MAX_ACTIVE_PIPELINES = 7;

        private readonly StationLevelData _stationLevels;
        private readonly FactionRoster _roster;
        private readonly Dictionary<string, Queue<ProductionQueueItem>> _productionQueues = new();
        private readonly Dictionary<(Type, string), int> _structureCounts = new();

        private SelectionType _selectionType;
        // Hero ships queued, in reserve or alive; each may exist only once until destroyed.
        private readonly HashSet<ShipType> _reservedHeroes = new();

        private int _currentLevel = 1;

        public event Action<IReadOnlyList<ProductionQueueSnapshot>> OnProductionChanged;

        public event Action<UnitRequest> OnUnitCompleted;

        public event Action<int> OnLevelUpgraded;
        public event Action<int> LevelUpgradeCompleted;

        public void CompleteLevelUpgrade()
        {
            CurrentLevel++;
            LevelUpgradeCompleted?.Invoke(CurrentLevel);
        }

        public event Action<SelectionType> OnSelectionTypeChanged;

        public SelectionType SelectionType
        {
            get => _selectionType;
            set
            {
                _selectionType = value;
                OnSelectionTypeChanged?.Invoke(_selectionType);
            }
        }

        public FactionType FactionType => _roster.FactionType;

        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                _currentLevel = value;
                OnLevelUpgraded?.Invoke(_currentLevel);
            }
        }

        public PlayerFactionModel(
            StationLevelData stationLevels,
            FactionRoster roster)
        {
            _stationLevels = stationLevels;
            _roster = roster;
        }

        public FactionData GetCurrentLevelFactionData()
        {
            return _stationLevels.GetLevelFactionData(CurrentLevel);
        }

        public bool CanQueueUnit(UnitRequest unitRequest)
        {
            if (IsUnitLimitReached(unitRequest)) return false;

            return _productionQueues.ContainsKey(unitRequest.Id) ||
                _productionQueues.Count < MAX_ACTIVE_PIPELINES;
        }

        public bool IsUnitLimitReached(UnitRequest unitRequest)
        {
            if (unitRequest == null)
            {
                throw new ArgumentNullException(nameof(unitRequest));
            }

            if (IsHeroRequest(unitRequest, out ShipType hero) && _reservedHeroes.Contains(hero)) return true;

            if (IsStructureRequest(unitRequest) &&
                GetStructureCount(unitRequest.GetType(), unitRequest.Id) >=
                unitRequest.FactionData.MaxCount)
            {
                return true;
            }

            return unitRequest.FactionData.MaxCount <= 0 ||
                (_productionQueues.TryGetValue(unitRequest.Id, out Queue<ProductionQueueItem> queue) &&
                    queue.Count >= unitRequest.FactionData.MaxCount);
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

            if (IsStructureRequest(unitRequest))
            {
                ReserveStructure(unitRequest);
            }

            if (!_productionQueues.TryGetValue(unitRequest.Id, out Queue<ProductionQueueItem> queue))
            {
                queue = new Queue<ProductionQueueItem>();
                _productionQueues.Add(unitRequest.Id, queue);
            }

            queue.Enqueue(new ProductionQueueItem(unitRequest));
            if (IsHeroRequest(unitRequest, out ShipType hero)) _reservedHeroes.Add(hero);
            NotifyProductionChanged();
        }

        /// <summary>Every queued item in pipeline order; only each pipeline's first item has started building.</summary>
        public IEnumerable<ProductionQueueItem> QueuedItems
        {
            get
            {
                foreach (Queue<ProductionQueueItem> queue in _productionQueues.Values)
                foreach (ProductionQueueItem item in queue)
                    yield return item;
            }
        }

        /// <summary>
        /// Queues saved items as they were, already paid for. Structures and hero ships take their reservations
        /// again, as a purchase does.
        /// </summary>
        public void RestoreQueue(IEnumerable<ProductionQueueItem> items)
        {
            foreach (ProductionQueueItem item in items)
            {
                UnitRequest unitRequest = item.UnitRequest;
                if (!_productionQueues.TryGetValue(unitRequest.Id, out Queue<ProductionQueueItem> queue))
                {
                    queue = new Queue<ProductionQueueItem>();
                    _productionQueues.Add(unitRequest.Id, queue);
                }

                queue.Enqueue(item);
                if (IsHeroRequest(unitRequest, out ShipType hero)) _reservedHeroes.Add(hero);
                if (IsStructureRequest(unitRequest)) ReserveStructure(unitRequest);
            }

            NotifyProductionChanged();
        }

        public void ReserveStructure(UnitRequest request)
        {
            var key = (request.GetType(), request.Id);
            _structureCounts[key] = GetStructureCount(request.GetType(), request.Id) + 1;
        }

        public bool TryCancelCurrentUnit(string id, out UnitRequest unitRequest)
        {
            if (!_productionQueues.TryGetValue(id, out Queue<ProductionQueueItem> queue))
            {
                unitRequest = null;
                return false;
            }

            unitRequest = queue.Dequeue().UnitRequest;
            if (IsHeroRequest(unitRequest, out ShipType hero)) _reservedHeroes.Remove(hero);
            if (IsStructureRequest(unitRequest))
            {
                ReleaseStructure(unitRequest);
            }
            if (queue.Count == 0)
            {
                _productionQueues.Remove(id);
            }

            NotifyProductionChanged();
            return true;
        }

        public void ReleaseStructure(UnitRequest unitRequest)
        {
            ReleaseStructure(unitRequest.GetType(), unitRequest.Id);
        }

        public bool IsHeroReserved(ShipType shipType) => _reservedHeroes.Contains(shipType);

        public bool IsHeroShip(ShipType shipType) =>
            _roster.Ships.TryGetValue(shipType, out FactionData data) && data.IsHero;

        public void SetHeroReserved(ShipType shipType, bool reserved)
        {
            if (reserved ? _reservedHeroes.Add(shipType) : _reservedHeroes.Remove(shipType))
                NotifyProductionChanged();
        }

        public static bool IsHeroRequest(UnitRequest unitRequest, out ShipType shipType)
        {
            shipType = unitRequest is ShipUnitRequest ship ? ship.Key : default;
            return unitRequest is ShipUnitRequest && unitRequest.FactionData.IsHero;
        }

        public void ReleaseStructure<TRequest>(string id) where TRequest : UnitRequest
        {
            ReleaseStructure(typeof(TRequest), id);
        }

        private void ReleaseStructure(Type requestType, string id)
        {
            var key = (requestType, id);
            int count = _structureCounts[key];
            if (count == 1)
            {
                _structureCounts.Remove(key);
            }
            else
            {
                _structureCounts[key] = count - 1;
            }
        }

        private int GetStructureCount(Type requestType, string id)
        {
            return _structureCounts.TryGetValue((requestType, id), out int count) ? count : 0;
        }

        private static bool IsStructureRequest(UnitRequest unitRequest)
        {
            return unitRequest is MiningFacilityUnitRequest ||
                unitRequest is DefendPlatformUnitRequest;
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
                    unitRequest: activeItem.UnitRequest,
                    count: queue.Count,
                    remainingBuildTime: activeItem.RemainingBuildTime));
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
