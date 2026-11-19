using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Models.Reinforcement;
using Unity.Profiling;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyProductionStrategy
    {
        private static readonly ProfilerMarker _evaluateMarker = new ProfilerMarker("EnemyAI.Production.Evaluate");

        private const float MINIMUM_PRODUCTION_INTERVAL = 1f;

        private const int SHIPS_PER_SQUADRON = 2;
        private const int CRITICAL_EXTRA_SQUADRONS = 2;
        private const int OPENING_SQUADRON_TARGET = 6;
        private const int DEFENSE_PLATFORM_TARGET = 1;

        private readonly IEnemyPurchaseProcessor _purchaseProcessor;
        private readonly IUnitRequestFactory _requestFactory;
        private readonly IEconomyModelObserver _economyModel;
        private readonly IEnemyAiStateProvider _stateProvider;
        private readonly IEnemyStructurePlacementService _enemyStructurePlacementService;
        private readonly IProductionAccess _productionAccess;

        private readonly EnemyFactionModel _factionModel;
        private readonly IFactionResearchModelObserver _research;
        private readonly PlayerSlot _owner;
        private readonly EnemyProductionDecisionModel _decisionModel;
        private readonly EnemyUnitLimitModel _unitLimitModel;
        private readonly ReinforcementData _reinforcementData;
        private readonly ForceCompositionBuilder _forceBuilder;
        private readonly UnitCombatProfileCatalog _profileCatalog;
        private readonly EnemyCounterProductionModel _counterModel;
        private readonly HostileIntelModel _intel;
        private readonly EnemyStationPressureTracker _stationPressure;
        private readonly EnemyAiScheduler _scheduler;

        private EnemyAiScheduler.WorkHandle _workHandle;

        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _hostileForce = new ForceComposition();
        private readonly Dictionary<UnitTypeId, int> _liveUnitCounts = new Dictionary<UnitTypeId, int>();
        private readonly List<ProductionCandidate> _candidates = new List<ProductionCandidate>();
        private readonly List<UnitTypeId> _candidateIds = new List<UnitTypeId>();
        private readonly List<FactionData> _candidateData = new List<FactionData>();

        private int _observedReleaseVersion;

        public float DefenseReserve { get; private set; }
        public bool PriorityConstructionPending
        {
            get => _purchaseProcessor.PriorityConstructionPending;
            set => _purchaseProcessor.PriorityConstructionPending = value;
        }

        public float OpeningLevelReserve => _factionModel.CurrentLevel == 1 && CanUpgradeStation
            ? _factionModel.GetCurrentLevelFactionData().Price : 0f;

        public bool OpeningMiningPending => IsOpening && CountReservedMiningFacilities() < GetMiningFacilityTarget();

        private bool IsOpening => _factionModel.CurrentLevel == 1 && _unitLimitModel.ShipOrdersCount == 0 &&
                                  !_purchaseProcessor.IsUpgradingStation;

        private bool CanUpgradeStation => _productionAccess.ProductionHost == _owner.Id &&
                                          _productionAccess.HasHostBase &&
                                          !_purchaseProcessor.IsUpgradingStation;

        public EnemyProductionStrategy(
            IEnemyPurchaseProcessor purchaseProcessor,
            IUnitRequestFactory requestFactory,
            IEconomyModelObserver economyModel,
            IEnemyAiStateProvider stateProvider,
            IEnemyStructurePlacementService enemyStructurePlacementService,
            IProductionAccess productionAccess,
            EnemyFactionModel factionModel,
            IFactionResearchModelObserver research,
            EnemyProductionDecisionModel decisionModel,
            EnemyUnitLimitModel unitLimitModel,
            ReinforcementData reinforcementData,
            ForceCompositionBuilder forceBuilder,
            UnitCombatProfileCatalog profileCatalog,
            EnemyCounterProductionModel counterModel,
            TeamIntelRegistry intelRegistry,
            PlayerSlot owner,
            EnemyStationPressureTracker stationPressure,
            EnemyAiScheduler scheduler)
        {
            _scheduler = scheduler;
            _owner = owner;
            _stationPressure = stationPressure;
            _forceBuilder = forceBuilder;
            _profileCatalog = profileCatalog;
            _counterModel = counterModel;
            _intel = intelRegistry.Get(owner.Team);
            _factionModel = factionModel;
            _research = research;
            _purchaseProcessor = purchaseProcessor;
            _requestFactory = requestFactory;
            _economyModel = economyModel;
            _stateProvider = stateProvider;
            _decisionModel = decisionModel;
            _unitLimitModel = unitLimitModel;
            _reinforcementData = reinforcementData;
            _enemyStructurePlacementService = enemyStructurePlacementService;
            _productionAccess = productionAccess;
        }

        public void Start()
        {
            _observedReleaseVersion = _unitLimitModel.ReleaseVersion;
            if (_workHandle == null)
            {
                EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(_owner.Difficulty);
                float interval = Mathf.Max(MINIMUM_PRODUCTION_INTERVAL, profile.DecisionInterval * 2f);
                _workHandle = _scheduler.Register(_owner.Id, interval, 0f, RunScheduledProduction);
            }
            else
            {
                _scheduler.MarkDue(_workHandle);
            }
        }

        public void Dispose()
        {
            if (_workHandle == null) return;
            _scheduler.Unregister(_workHandle);
            _workHandle = null;
        }

        public void Tick(float deltaTime)
        {
            if (_workHandle == null || !_productionAccess.CanProduce)
            {
                return;
            }

            if (_observedReleaseVersion != _unitLimitModel.ReleaseVersion)
            {
                _observedReleaseVersion = _unitLimitModel.ReleaseVersion;
                _scheduler.MarkDue(_workHandle);
            }
        }

        private void RunScheduledProduction()
        {
            if (!_productionAccess.CanProduce)
            {
                return;
            }

            EvaluateProduction();
        }

        private void EvaluateProduction()
        {
            using var sample = _evaluateMarker.Auto();
            int shipCount = CountReservedShips();
            int miningFacilityCount = CountReservedMiningFacilities();
            int defensePlatformCount = CountReservedDefensePlatforms();
            bool canPlaceStructure = _enemyStructurePlacementService.TryGetPosition(out _);
            bool hasMiningSelection = TrySelectMiningFacility(
                out KeyValuePair<MiningFacilityType, FactionData> mining);
            bool hasMiningOption = canPlaceStructure && hasMiningSelection;
            bool canBuildMining = hasMiningOption && IsAffordable(mining.Value);
            bool hasDefenseSelection = TrySelectDefense(
                out KeyValuePair<DefendPlatformType, FactionData> defense);
            bool canBuildDefense = canPlaceStructure && hasDefenseSelection && IsAffordable(defense.Value);

            KeyValuePair<SquadronType, FactionData> squadron = default;
            KeyValuePair<ShipType, FactionData> ship = default;
            bool atOpeningLevel = _factionModel.CurrentLevel == 1;
            bool allowShips = !atOpeningLevel || _unitLimitModel.ShipOrdersCount == 0 && shipCount == 0;
            bool canOrderShips = allowShips && CanOrderAnyShip();
            bool hasSquadronRoom = atOpeningLevel
                ? CountReservedSquadrons() < OPENING_SQUADRON_TARGET
                : HasSquadronRoom(shipCount);
            UnitTypeId counterId = default;
            FactionData counterData = null;
            bool hasCounter = (!atOpeningLevel || _stationPressure.IsStationCritical) && TrySelectCounterUnit(
                !_stationPressure.IsStationCritical &&
                canOrderShips && (_decisionModel.NeedsMinimumFleet(shipCount) || !hasSquadronRoom),
                hasSquadronRoom, allowShips, out counterId, out counterData);
            bool hasShip = !hasCounter && canOrderShips && TrySelectShip(shipCount, out ship);
            bool canBuyOpeningShip = atOpeningLevel && hasShip && !_purchaseProcessor.IsUpgradingStation &&
                                     IsAffordable(ship.Value) && FitsRemainingCapacity(ship.Value);
            if (atOpeningLevel && !_stationPressure.IsStationCritical)
                hasShip = canBuyOpeningShip;
            bool hasSquadronFallback = !_stationPressure.IsStationCritical && !hasCounter && !hasShip &&
                                       (atOpeningLevel ? hasSquadronRoom : !canOrderShips) &&
                                       TrySelectSquadron(shipCount, true, out squadron);
            bool hasShipOption = hasCounter || hasShip || hasSquadronFallback;
            FactionData shipChoice = hasCounter ? counterData : hasShip ? ship.Value : squadron.Value;
            DefenseReserve = hasShipOption ? shipChoice.Price : 0f;
            bool canBuildShip = hasShipOption && IsAffordable(shipChoice) && FitsRemainingCapacity(shipChoice);
            UnitTypeId combatChoice = hasCounter ? counterId : hasShip
                ? UnitTypeId.Ship(ship.Key) : UnitTypeId.Squadron(squadron.Key);
            if (_stationPressure.IsStationCritical && !hasShipOption)
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "production-wait",
                    FormattableString.Invariant($"critical=True;etaSeconds=Infinity;timeToLoss={_stationPressure.EstimatedTimeToLossSeconds:F1};") +
                    "reason=no affordable combat unit can arrive before station loss");
            }
            if (hasShipOption && !canBuildShip)
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "production-wait",
                    FormattableString.Invariant($"unit={combatChoice};capacity={_unitLimitModel.CurrentUnitCapacity}/{_reinforcementData.MaxUnitCapacity};required={shipChoice.UnitCapacity};cost={shipChoice.Price};money={_economyModel.Money:F1};critical={_stationPressure.IsStationCritical};etaSeconds={GetArrivalSeconds(combatChoice, shipChoice):F1};timeToLoss={_stationPressure.EstimatedTimeToLossSeconds:F1}"));
            }
            FactionData levelData = CanUpgradeStation ? _factionModel.GetCurrentLevelFactionData() : null;
            if (levelData != null &&
                !_purchaseProcessor.CanQueue(_factionModel.CurrentLevel.ToString(), levelData.MaxCount))
            {
                levelData = null;
            }
            bool hasLevelUpOption = levelData != null;
            bool canLevelUp = levelData != null && levelData.Price <= _economyModel.Money;
            bool canResearchIncome = TrySelectResearch(true, out ResearchType incomeResearch,
                                         out ResearchTierData incomeTier) &&
                                     IsAffordable(incomeTier.FactionData);
            bool canResearchCombat = TrySelectResearch(false, out ResearchType combatResearch,
                                         out ResearchTierData combatTier) &&
                                     IsAffordable(combatTier.FactionData);
            int miningFacilityTarget = GetMiningFacilityTarget();

            if (!_stationPressure.IsStationCritical && TryBuyScout(shipCount,
                    hasMiningSelection && miningFacilityCount < miningFacilityTarget,
                    canPlaceStructure))
            {
                return;
            }

            var snapshot = new EnemyProductionSnapshot(
                _stateProvider.CurrentState,
                miningFacilityCount,
                shipCount,
                _unitLimitModel.ShipOrdersCount,
                defensePlatformCount,
                _factionModel.CurrentLevel,
                miningFacilityTarget,
                DEFENSE_PLATFORM_TARGET,
                hasMiningOption,
                hasShipOption,
                canBuildShip,
                canBuildMining,
                canBuildDefense,
                hasLevelUpOption,
                canLevelUp,
                canResearchIncome,
                canResearchCombat,
                canOrderShips,
                _stationPressure.IsStationCritical,
                canBuyOpeningShip,
                IsOpening);
            EnemyProductionCategory category = _decisionModel.Evaluate(snapshot);
            if (PriorityConstructionPending && !_stationPressure.IsStationCritical &&
                category is not (EnemyProductionCategory.Mining or EnemyProductionCategory.Level))
            {
                return;
            }
            if (category == EnemyProductionCategory.None && _decisionModel.IsLevelDue(snapshot))
                DefenseReserve = levelData.Price;

            bool buildSquadron = !_stationPressure.IsStationCritical && !hasCounter &&
                                 category == EnemyProductionCategory.Ship &&
                                 (hasSquadronFallback ||
                                  !atOpeningLevel && TrySelectSquadron(shipCount, false, out squadron) &&
                                  IsAffordable(squadron.Value));
            UnitRequest request = category switch
            {
                EnemyProductionCategory.Ship when hasCounter && counterId.IsShip =>
                    _requestFactory.ConstructUnitRequest(counterData, counterId.ShipType),
                EnemyProductionCategory.Ship when hasCounter =>
                    _requestFactory.ConstructUnitRequest(counterData, counterId.SquadronType),
                EnemyProductionCategory.Ship when buildSquadron =>
                    _requestFactory.ConstructUnitRequest(squadron.Value, squadron.Key),
                EnemyProductionCategory.Ship =>
                    _requestFactory.ConstructUnitRequest(ship.Value, ship.Key),
                EnemyProductionCategory.Mining =>
                    _requestFactory.ConstructUnitRequest(mining.Value, mining.Key),
                EnemyProductionCategory.Defense =>
                    _requestFactory.ConstructUnitRequest(defense.Value, defense.Key),
                EnemyProductionCategory.Level =>
                    _requestFactory.ConstructUnitRequest(levelData, _factionModel.CurrentLevel),
                EnemyProductionCategory.Research when canResearchIncome =>
                    CreateResearchRequest(incomeResearch, incomeTier),
                EnemyProductionCategory.Research =>
                    CreateResearchRequest(combatResearch, combatTier),
                EnemyProductionCategory.None => null,
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };

            if (request == null)
            {
                return;
            }

            DefenseReserve = request.FactionData.Price;

            Debug.Log(
                $"[EnemyAI:Production] BattleTime={BattleClock.CurrentElapsed:F1}, Player={_owner.Id}:{_owner.Faction}, State={_stateProvider.CurrentState}, " +
                $"FactionLevel={_factionModel.CurrentLevel}, Category={category}, Counter={hasCounter}, " +
                $"Unit={request.Id}, Mining={miningFacilityCount}/{miningFacilityTarget}, " +
                $"Defense={defensePlatformCount}/{DEFENSE_PLATFORM_TARGET}, " +
                $"Cost={request.FactionData.Price}, " +
                $"Money={_economyModel.Money}");
            EnemyAiTelemetry.RecordPurchase(_owner.Id, _owner.Faction, _stateProvider.CurrentState, category,
                hasCounter, request.Id, request.FactionData.Price, _economyModel.Money,
                _stationPressure.IsStationCritical,
                category == EnemyProductionCategory.Ship ? GetArrivalSeconds(
                    buildSquadron ? UnitTypeId.Squadron(squadron.Key) : combatChoice,
                    request.FactionData) : 0f,
                _stationPressure.EstimatedTimeToLossSeconds);
            _purchaseProcessor.Purchase(request);
        }

        private int GetMiningFacilityTarget()
        {
            int target = 0;
            foreach (FactionData mining in _factionModel.MiningFactions.Values)
                if (IsAvailable(mining)) target += mining.MaxCount;
            return target;
        }

        private bool FitsRemainingCapacity(FactionData data) =>
            data.UnitCapacity <= _reinforcementData.MaxUnitCapacity - _unitLimitModel.CurrentUnitCapacity;

        private int CountReservedShips()
        {
            int count = 0;
            foreach (KeyValuePair<ShipType, FactionData> option
                     in _factionModel.ShipFactionData)
            {
                count += _unitLimitModel.GetReservedCount<ShipUnitRequest>(
                    option.Key.ToString());
            }

            return count;
        }

        private int CountReservedMiningFacilities()
        {
            int count = 0;
            foreach (KeyValuePair<MiningFacilityType, FactionData> option
                     in _factionModel.MiningFactions)
            {
                count += _unitLimitModel.GetReservedCount<MiningFacilityUnitRequest>(
                    option.Key.ToString());
            }

            return count;
        }

        private int CountReservedDefensePlatforms()
        {
            int count = 0;
            foreach (KeyValuePair<DefendPlatformType, FactionData> option
                     in _factionModel.DefendPlatforms)
            {
                count += _unitLimitModel.GetReservedCount<DefendPlatformUnitRequest>(
                    option.Key.ToString());
            }

            return count;
        }

        private bool TrySelectMiningFacility(
            out KeyValuePair<MiningFacilityType, FactionData> selected)
        {
            selected = default;
            bool found = false;
            foreach (KeyValuePair<MiningFacilityType, FactionData> option
                     in _factionModel.MiningFactions)
            {
                if (option.Value.AvailableLevel > _factionModel.CurrentLevel ||
                    !CanReserve<MiningFacilityUnitRequest>(
                        option.Key.ToString(),
                        option.Value))
                {
                    continue;
                }

                if (!found || option.Value.Price < selected.Value.Price)
                {
                    selected = option;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Rates every buildable ship and squadron against all hostile units; units already bought but not yet
        /// in play count toward the own force so the same counter is not bought twice.
        /// </summary>
        private bool TrySelectCounterUnit(
            bool shipsOnly,
            bool hasSquadronRoom,
            bool allowShips,
            out UnitTypeId selectedId,
            out FactionData selectedData)
        {
            selectedId = default;
            selectedData = null;
            PlayerId self = _owner.Id;
            // Counters answer what the team has seen of the enemy, not the whole hidden map.
            _forceBuilder.BuildKnown(_hostileForce, _intel, Time.time, sighting => true);
            if (_hostileForce.IsEmpty && !_stationPressure.IsStationCritical)
                return false;
            _forceBuilder.Build(_ownForce, entity => entity.Owner == self, _liveUnitCounts);
            _candidates.Clear();
            _candidateIds.Clear();
            _candidateData.Clear();
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
            {
                UnitTypeId unitId = UnitTypeId.Ship(option.Key);
                AddCounterOption<ShipUnitRequest>(unitId, option.Key.ToString(), option.Value,
                    allowShips && IsAvailable(option.Value) && IsShipClassAllowed(unitId), hasSquadronRoom);
            }

            foreach (KeyValuePair<SquadronType, FactionData> option in _factionModel.SquadronFactionData)
            {
                AddCounterOption<SquadronUnitRequest>(UnitTypeId.Squadron(option.Key), option.Key.ToString(),
                    option.Value, !shipsOnly, hasSquadronRoom);
            }

            if (!_counterModel.TrySelect(_ownForce, _hostileForce, _candidates, out int index,
                    _economyModel.Money,
                    _reinforcementData.MaxUnitCapacity - _unitLimitModel.CurrentUnitCapacity,
                    _stationPressure.IsStationCritical, _stationPressure.EstimatedTimeToLossSeconds))
            {
                return false;
            }

            selectedId = _candidateIds[index];
            selectedData = _candidateData[index];
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "counter-selection",
                $"unit={selectedId};candidates={_candidates.Count};score={_counterModel.SelectedScore:F6};" +
                $"strikecraftCovered={_counterModel.StrikecraftCovered};" +
                $"capacity={_unitLimitModel.CurrentUnitCapacity}/{_reinforcementData.MaxUnitCapacity};" +
                $"required={selectedData.UnitCapacity};cost={selectedData.Price};money={_economyModel.Money}");
            return true;
        }

        /// <summary>
        /// Counts pending units of this type into the own force; adds it as a candidate when
        /// <paramref name="isSelectable"/>. A carrier's hangar is valued only while squadrons have room, because
        /// hangar squadrons share the squadron cap.
        /// </summary>
        private void AddCounterOption<TRequest>(
            UnitTypeId unitTypeId,
            string requestId,
            FactionData data,
            bool isSelectable,
            bool hasSquadronRoom)
        {
            _liveUnitCounts.TryGetValue(unitTypeId, out int liveCount);
            int reservedCount = _unitLimitModel.GetReservedCount<TRequest>(requestId);
            int pendingCount = reservedCount - liveCount;
            for (int i = 0; i < pendingCount; i++)
            {
                _ownForce.AddNew(_profileCatalog.Get(unitTypeId), true);
            }

            // Select the next reinforcement before checking free population. Otherwise every small
            // loss gets replaced by a cheap ship and a selected capital can never fit.
            if (!isSelectable || !IsAvailable(data) || !CanReserve<TRequest>(requestId, data, true))
            {
                return;
            }

            if (_stationPressure.IsStationCritical && (!IsAffordable(data) ||
                _unitLimitModel.CurrentUnitCapacity + data.UnitCapacity > _reinforcementData.MaxUnitCapacity)) return;

            _candidates.Add(new ProductionCandidate(_profileCatalog.Get(unitTypeId), data.Price,
                Math.Max(reservedCount, liveCount), hasSquadronRoom, data.UnitCapacity,
                GetArrivalSeconds(unitTypeId, data)));
            _candidateIds.Add(unitTypeId);
            _candidateData.Add(data);
        }

        private bool TrySelectShip(
            int shipCount,
            out KeyValuePair<ShipType, FactionData> selected)
        {
            selected = default;
            bool found = false;
            float bestPriority = float.MaxValue;
            foreach (KeyValuePair<ShipType, FactionData> option
                     in _factionModel.ShipFactionData)
            {
                // Affordability is checked after selection so the AI saves for higher-tier ships.
                UnitTypeId unitType = UnitTypeId.Ship(option.Key);
                if (!IsAvailable(option.Value) || !IsShipClassAllowed(unitType) ||
                    !CanReserve<ShipUnitRequest>(
                        option.Key.ToString(),
                        option.Value, true))
                {
                    continue;
                }

                if (_stationPressure.IsStationCritical && (!IsAffordable(option.Value) ||
                    _unitLimitModel.CurrentUnitCapacity + option.Value.UnitCapacity > _reinforcementData.MaxUnitCapacity)) continue;

                int reservedCount = _unitLimitModel.GetReservedCount<ShipUnitRequest>(
                    option.Key.ToString());
                float combatValuePerCredit = _profileCatalog.Get(unitType).GetCombatValue() /
                    Math.Max(1, option.Value.Price);
                float priority = _decisionModel.CalculateShipPriority(
                    _stateProvider.CurrentState,
                    shipCount,
                    reservedCount,
                    option.Value.BuildTime,
                    option.Value.UnitCapacity,
                    option.Value.MaxCount,
                    combatValuePerCredit);
                if (_stationPressure.IsStationCritical)
                {
                    if (combatValuePerCredit <= 0f) continue;
                    float arrival = GetArrivalSeconds(unitType, option.Value);
                    if (float.IsPositiveInfinity(arrival) || !(arrival <= _stationPressure.EstimatedTimeToLossSeconds))
                        continue;
                    priority = Math.Max(1f, arrival) / combatValuePerCredit;
                }
                if (!found || priority < bestPriority ||
                    priority == bestPriority && option.Value.Price < selected.Value.Price)
                {
                    selected = option;
                    bestPriority = priority;
                    found = true;
                }
            }

            return found;
        }

        private float GetArrivalSeconds(UnitTypeId unitTypeId, FactionData data)
        {
            if (!_stationPressure.IsStationCritical) return 0f;
            float travel = 0f;
            if (_stationPressure.HasThreatPosition &&
                _productionAccess.TryGetProductionBase(out var productionBase))
            {
                Vector3 threat = new Vector3(_stationPressure.ThreatX, _stationPressure.ThreatY, _stationPressure.ThreatZ);
                Vector3 offset = threat - productionBase.GetFacade<IEntityTransformFacade>().Transform.position;
                offset.y = 0f;
                travel = unitTypeId.IsShip
                    ? _profileCatalog.GetShipTravelTime(unitTypeId.ShipType, offset.magnitude)
                    : _profileCatalog.GetSquadronTravelTime(unitTypeId.SquadronType, offset.magnitude);
            }
            return _purchaseProcessor.GetQueueRemainingTime(unitTypeId.ToString()) + data.BuildTime + travel;
        }

        /// <summary>True while bought squadrons number fewer than one per <see cref="SHIPS_PER_SQUADRON"/> ships.</summary>
        /// <remarks>Carrier hangars count toward the cap so carriers are no way around it.</remarks>
        private bool HasSquadronRoom(int shipCount)
        {
            int cap = shipCount / SHIPS_PER_SQUADRON;
            // A critical station may buy more wings for immediate defense, but still within a cap.
            if (_stationPressure.IsStationCritical) cap = cap * 2 + CRITICAL_EXTRA_SQUADRONS;
            return cap > 0 && CountReservedSquadrons() + CountHangarSquadrons() < cap;
        }

        /// <summary>Live wings, usable carrier reserves and initial wings of ships still awaiting deployment.</summary>
        private int CountHangarSquadrons()
        {
            int count = _forceBuilder.CountHangarSquadrons(_owner.Id, _liveUnitCounts);
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
            {
                UnitTypeId unitType = UnitTypeId.Ship(option.Key);
                _liveUnitCounts.TryGetValue(unitType, out int liveCount);
                int reserved = _unitLimitModel.GetReservedCount<ShipUnitRequest>(option.Key.ToString());
                int pending = reserved - liveCount;
                if (pending > 0)
                {
                    count += pending * _profileCatalog.Get(unitType).Hangar.Count;
                }
            }

            return count;
        }

        private int CountReservedSquadrons()
        {
            int squadronCount = 0;
            foreach (KeyValuePair<SquadronType, FactionData> option
                     in _factionModel.SquadronFactionData)
            {
                squadronCount += _unitLimitModel.GetReservedCount<SquadronUnitRequest>(
                    option.Key.ToString());
            }

            return squadronCount;
        }

        /// <summary>True when any ship type is unlocked at the current level and within its limits.</summary>
        private bool CanOrderAnyShip()
        {
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
            {
                if (IsAvailable(option.Value) && IsShipClassAllowed(UnitTypeId.Ship(option.Key)) &&
                    CanReserve<ShipUnitRequest>(option.Key.ToString(), option.Value, true))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Mining has no visible place to go: one fighter is bought to scout the fog for build space.
        /// The squadron commander flies a squadron to the scout target.
        /// </summary>
        private bool TryBuyScout(int shipCount, bool needsMining, bool canPlaceStructure)
        {
            if (!needsMining ||
                canPlaceStructure ||
                CountReservedSquadrons() > 0 ||
                !_enemyStructurePlacementService.TryGetScoutTarget(out _) ||
                !TrySelectSquadron(shipCount, true, out KeyValuePair<SquadronType, FactionData> scout) ||
                !IsAffordable(scout.Value))
            {
                return false;
            }

            UnitRequest request = _requestFactory.ConstructUnitRequest(scout.Value, scout.Key);
            Debug.Log(
                $"[EnemyAI:Production] BattleTime={BattleClock.CurrentElapsed:F1}, Player={_owner.Id}:{_owner.Faction}, Category=Scout, Unit={request.Id}, " +
                $"Cost={request.FactionData.Price}, Money={_economyModel.Money}");
            _purchaseProcessor.Purchase(request);
            return true;
        }

        /// <summary>Picks the cheapest available squadron while <see cref="HasSquadronRoom"/>, or regardless of the ship ratio.</summary>
        private bool TrySelectSquadron(
            int shipCount,
            bool ignoreShipRatio,
            out KeyValuePair<SquadronType, FactionData> selected)
        {
            selected = default;
            if (!ignoreShipRatio && !HasSquadronRoom(shipCount))
            {
                return false;
            }

            bool found = false;
            foreach (KeyValuePair<SquadronType, FactionData> option
                     in _factionModel.SquadronFactionData)
            {
                if (!IsAvailable(option.Value) ||
                    !CanReserve<SquadronUnitRequest>(
                        option.Key.ToString(),
                        option.Value))
                {
                    continue;
                }

                if (!found || option.Value.Price < selected.Value.Price)
                {
                    selected = option;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Picks the strongest (most expensive) platform the AI can pay for now, so Golan III and Empress are built once
        /// affordable; with none affordable, the cheapest one is the one to save for.
        /// </summary>
        private bool TrySelectDefense(
            out KeyValuePair<DefendPlatformType, FactionData> selected)
        {
            selected = default;
            bool found = false;
            foreach (KeyValuePair<DefendPlatformType, FactionData> option
                     in _factionModel.DefendPlatforms)
            {
                if (!IsAvailable(option.Value) ||
                    !CanReserve<DefendPlatformUnitRequest>(
                        option.Key.ToString(),
                        option.Value))
                {
                    continue;
                }

                bool isAffordable = IsAffordable(option.Value);
                bool isSelectedAffordable = found && IsAffordable(selected.Value);
                bool isBetter = !found || (isAffordable
                    ? !isSelectedAffordable || option.Value.Price > selected.Value.Price
                    : !isSelectedAffordable && option.Value.Price < selected.Value.Price);
                if (isBetter)
                {
                    selected = option;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Picks the cheapest unlocked next tier, not already being researched, among the lines that raise income
        /// (<paramref name="raisesIncome"/>) or among the combat lines.
        /// </summary>
        private bool TrySelectResearch(
            bool raisesIncome,
            out ResearchType selectedType,
            out ResearchTierData selectedTier)
        {
            selectedType = default;
            selectedTier = null;
            foreach (ResearchType researchType in _research.ResearchTypes)
            {
                if (!_research.TryGetNextTier(researchType, out ResearchTierData tier) ||
                    RaisesIncome(tier) != raisesIncome ||
                    !IsAvailable(tier.FactionData) ||
                    !CanReserve<ResearchUnitRequest>(researchType.ToString(), tier.FactionData))
                {
                    continue;
                }

                if (selectedTier == null || tier.FactionData.Price < selectedTier.FactionData.Price)
                {
                    selectedType = researchType;
                    selectedTier = tier;
                }
            }

            return selectedTier != null;
        }

        private static bool RaisesIncome(ResearchTierData tier)
        {
            foreach (ResearchEffect effect in tier.Effects)
            {
                if (effect.Stat == ResearchStat.Income || effect.Stat == ResearchStat.StationIncome)
                {
                    return true;
                }
            }

            return false;
        }

        private UnitRequest CreateResearchRequest(ResearchType researchType, ResearchTierData tier)
        {
            return _requestFactory.ConstructUnitRequest(
                tier.FactionData,
                researchType,
                _research.GetCompletedTiers(researchType) + 1);
        }

        private bool CanReserve<TRequest>(string requestId, FactionData data, bool ignoreUsedCapacity = false)
        {
            return _purchaseProcessor.CanQueue(requestId, data.MaxCount) &&
                _unitLimitModel.GetReservedCount<TRequest>(requestId) < data.MaxCount &&
                data.UnitCapacity <= _reinforcementData.MaxUnitCapacity -
                    (ignoreUsedCapacity ? 0 : _unitLimitModel.CurrentUnitCapacity);
        }

        private bool IsShipClassAllowed(UnitTypeId shipId) =>
            _factionModel.CurrentLevel != 1 || _profileCatalog.Get(shipId).ShipClass == ShipClass.Corvette;

        private bool IsAvailable(FactionData data)
        {
            return data.AvailableLevel <= _factionModel.CurrentLevel;
        }

        private bool IsAffordable(FactionData data)
        {
            return data.Price <= _economyModel.Money;
        }
    }
}
