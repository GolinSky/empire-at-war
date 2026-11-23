using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Factions
{
    public interface IPlayerFactionModelObserver : IModelObserver, IFactionLevelObserver
    {
        event Action<IReadOnlyList<ProductionQueueSnapshot>> OnProductionChanged;

        event Action<UnitRequest> OnUnitCompleted;

        event Action<SelectionType> OnSelectionTypeChanged;

        SelectionType SelectionType { get; }
        /// <summary>The selected base is the second station, so its own level-up card shows instead of the main one.</summary>
        bool IsSecondStationSelected { get; }
        bool IsUniqueReserved(UnitTypeId unitType);
        bool IsUnitLimitReached(UnitRequest unitRequest);
        bool CanQueueUnit(UnitRequest unitRequest);
        FactionType FactionType { get; }

        FactionData GetCurrentLevelFactionData();

        IReadOnlyList<ProductionQueueSnapshot> GetProductionQueueSnapshots();
    }

    public class PlayerFactionModel : Model, IPlayerFactionModelObserver
    {
        public const int MAX_ACTIVE_PIPELINES = 7;
        private const int STANDARD_UNIT_QUEUE_LIMIT = 10;

        private readonly StationLevelData _stationLevels;
        private readonly FactionRoster _roster;
        private readonly Dictionary<string, Queue<ProductionQueueItem>> _productionQueues = new();
        private readonly Dictionary<(Type, string), int> _structureCounts = new();

        private SelectionType _selectionType;
        // Unique ships and squadrons (heroes, dreadnoughts) queued, in reserve or alive; each may exist only once until destroyed.
        private readonly HashSet<UnitTypeId> _reservedUniqueUnits = new();

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

        /// <summary>Set before <see cref="SelectionType"/>, whose change refreshes the panel.</summary>
        public bool IsSecondStationSelected { get; set; }

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

            if (IsUniqueRequest(unitRequest, out UnitTypeId unique) && _reservedUniqueUnits.Contains(unique)) return true;

            if (IsStructureRequest(unitRequest) &&
                GetStructureCount(unitRequest.GetType(), unitRequest.Id) >=
                unitRequest.FactionData.MaxCount)
            {
                return true;
            }

            int queueLimit = GetQueueLimit(unitRequest);
            return queueLimit <= 0 ||
                (_productionQueues.TryGetValue(unitRequest.Id, out Queue<ProductionQueueItem> queue) &&
                    queue.Count >= queueLimit);
        }

        internal static int GetQueueLimit(UnitRequest unitRequest) =>
            GetQueueLimit(unitRequest.FactionData, unitRequest is ShipUnitRequest or SquadronUnitRequest);

        /// <summary>Per-type queue limit; <paramref name="isCombatUnit"/> = ship or squadron. Shared with the AI (W69).</summary>
        internal static int GetQueueLimit(FactionData data, bool isCombatUnit)
        {
            if (data.MaxCount <= 1 || !isCombatUnit)
                return data.MaxCount;

            return data.IsUnique ? 1 : STANDARD_UNIT_QUEUE_LIMIT;
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
            if (IsUniqueRequest(unitRequest, out UnitTypeId unique)) _reservedUniqueUnits.Add(unique);
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
        /// Queues saved items as they were, already paid for. Structures and unique ships take their reservations
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
                if (IsUniqueRequest(unitRequest, out UnitTypeId unique)) _reservedUniqueUnits.Add(unique);
                if (IsStructureRequest(unitRequest)) ReserveStructure(unitRequest);
            }

            NotifyProductionChanged();
        }

        /// <summary>
        /// Online client: restores the host's queue and complete purchase reservations atomically.
        /// Queue removal may mean completion, so it cannot release a reserved or deployed unit's slot.
        /// </summary>
        public void ReplaceQueue(IEnumerable<ProductionQueueItem> items, IEnumerable<UnitRequest> reservations)
        {
            _productionQueues.Clear();
            _structureCounts.Clear();
            _reservedUniqueUnits.Clear();
            foreach (UnitRequest request in reservations)
            {
                if (IsUniqueRequest(request, out UnitTypeId unique)) _reservedUniqueUnits.Add(unique);
                if (IsStructureRequest(request)) ReserveStructure(request);
            }

            foreach (ProductionQueueItem item in items)
            {
                string id = item.UnitRequest.Id;
                if (!_productionQueues.TryGetValue(id, out Queue<ProductionQueueItem> queue))
                {
                    queue = new Queue<ProductionQueueItem>();
                    _productionQueues.Add(id, queue);
                }
                queue.Enqueue(item);
            }

            NotifyProductionChanged();
        }

        /// <summary>Purchase slots retained by queued, reserve and deployed structures or unique units.</summary>
        public IEnumerable<UnitRequest> PurchaseReservations
        {
            get
            {
                foreach (UnitTypeId unique in _reservedUniqueUnits)
                    yield return unique.IsShip
                        ? new ShipUnitRequest(_roster.Ships[unique.ShipType], unique.ShipType)
                        : new SquadronUnitRequest(_roster.Squadrons[unique.SquadronType], unique.SquadronType);
                foreach (var entry in _roster.MiningFacilities)
                {
                    var request = new MiningFacilityUnitRequest(entry.Value, entry.Key);
                    for (int i = 0; i < GetStructureCount(request.GetType(), request.Id); i++)
                        yield return request;
                }
                foreach (var entry in _roster.DefendPlatforms)
                {
                    var request = new DefendPlatformUnitRequest(entry.Value, entry.Key);
                    for (int i = 0; i < GetStructureCount(request.GetType(), request.Id); i++)
                        yield return request;
                }
                var secondStation = new SecondStationUnitRequest(_stationLevels.GetSecondStationFactionData(FactionType));
                for (int i = 0; i < GetStructureCount(secondStation.GetType(), secondStation.Id); i++)
                    yield return secondStation;
            }
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
            if (IsUniqueRequest(unitRequest, out UnitTypeId unique)) _reservedUniqueUnits.Remove(unique);
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

        public bool IsUniqueReserved(UnitTypeId unitType) => _reservedUniqueUnits.Contains(unitType);

        public bool IsUniqueUnit(UnitTypeId unitType)
        {
            FactionData data = null;
            bool found = unitType.IsShip
                ? _roster.Ships.TryGetValue(unitType.ShipType, out data)
                : _roster.Squadrons.TryGetValue(unitType.SquadronType, out data);
            return found && data.IsUnique;
        }

        public void SetUniqueReserved(UnitTypeId unitType, bool reserved)
        {
            if (reserved ? _reservedUniqueUnits.Add(unitType) : _reservedUniqueUnits.Remove(unitType))
                NotifyProductionChanged();
        }

        public static bool IsUniqueRequest(UnitRequest unitRequest, out UnitTypeId unitType)
        {
            switch (unitRequest)
            {
                case ShipUnitRequest ship:
                    unitType = UnitTypeId.Ship(ship.Key);
                    break;
                case SquadronUnitRequest squadron:
                    unitType = UnitTypeId.Squadron(squadron.Key);
                    break;
                default:
                    unitType = default;
                    return false;
            }

            return unitRequest.FactionData.IsUnique;
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
                unitRequest is DefendPlatformUnitRequest ||
                unitRequest is SecondStationUnitRequest;
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
