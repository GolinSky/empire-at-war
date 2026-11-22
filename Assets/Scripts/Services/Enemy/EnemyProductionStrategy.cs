using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
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
        private const int MINIMUM_CAPTURE_WINGS = 3;
        private const float COMBAT_RESEARCH_INTERVAL = 30f;
        private const float DEFENSE_BUILD_INTERVAL = 20f;
        private const float STATION_PLACEMENT_RETRY_SECONDS = 10f;
        private const int MAX_LEVEL1_CORVETTES = 2;
        /// <summary>Bombers are wanted only to raid: at most this many per known hostile mine.</summary>
        private const int BOMBERS_PER_HOSTILE_MINE = 3;
        /// <summary>Wings capture the opening; the first corvette may be bought from this battle time.</summary>
        public const float OPENING_NO_CORVETTE_SECONDS = 30f;
        private const float NON_COMPETING_RESEARCH_SHARE = 0.25f;
        private const float PATROL_WING_RETRY_SECONDS = 60f;
        private const int SAVING_MIN_SHIPS = 10;
        /// <summary>Priority penalty (lower builds first) for a ship that does not fit the free unit capacity.</summary>
        private const float NO_FIT_SAVING_PRIORITY_SCALE = 2f;
        private const float SAVING_MIN_ADVANTAGE = 1f;
        private const float SURPLUS_SHIP_FACTOR = 2f;
        /// <summary>Ships more than this many levels below the station level are no longer bought.</summary>
        private const int SHIP_TIER_SPAN = 1;
        private const float MINER_REBUY_INTERVAL = 10f;

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
        private readonly IEnemySquadronCommander _squadrons;
        private readonly SecondStationLevelModel _secondStation;

        private EnemyAiScheduler.WorkHandle _workHandle;

        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _hostileForce = new ForceComposition();
        private readonly Dictionary<UnitTypeId, int> _liveUnitCounts = new Dictionary<UnitTypeId, int>();
        private readonly List<ProductionCandidate> _candidates = new List<ProductionCandidate>();
        private readonly List<(KeyValuePair<ShipType, FactionData> Option, float Arrival, float PerCredit, float PerSlot)>
            _criticalShips = new List<(KeyValuePair<ShipType, FactionData>, float, float, float)>();
        private readonly List<UnitTypeId> _candidateIds = new List<UnitTypeId>();
        private readonly List<FactionData> _candidateData = new List<FactionData>();

        private int _observedReleaseVersion;
        private float _nextCombatResearchTime;
        private float _nextDefenseBuildTime;
        private float _nextStationPlacementCheck;
        private string _savingUnit = "";
        /// <summary>The last evaluation's ship choice was affordable but blocked by the unit cap (W34 b).</summary>
        private bool _capacityBlocked;
        private float _savingSince = -1f;
        private float _patrolWingRetryAt = float.NegativeInfinity;
        private int _peakMiningCount;
        private float _nextMinerRebuyTime = float.NegativeInfinity;

        public float DefenseReserve { get; private set; }
        public bool PriorityConstructionPending
        {
            get => _purchaseProcessor.PriorityConstructionPending;
            set => _purchaseProcessor.PriorityConstructionPending = value;
        }

        public float OpeningLevelReserve => _factionModel.CurrentLevel == 1 && CanUpgradeStation
            ? _factionModel.GetCurrentLevelFactionData().Price : 0f;

        public bool OpeningMiningPending => _factionModel.CurrentLevel == 1 &&
                                            CountReservedMiningFacilities() < GetMiningFacilityTarget();

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
            EnemyAiScheduler scheduler,
            IEnemySquadronCommander squadrons,
            SecondStationLevelModel secondStation)
        {
            _secondStation = secondStation;
            _squadrons = squadrons;
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
            if (TryBuyPatrolWing()) return;
            int shipCount = CountReservedShips();
            int miningFacilityCount = CountReservedMiningFacilities();
            int defensePlatformCount = CountReservedDefensePlatforms();
            bool canPlaceStructure = _enemyStructurePlacementService.TryGetPosition(out _);
            bool hasMiningSelection = TrySelectMiningFacility(
                out KeyValuePair<MiningFacilityType, FactionData> mining);
            // Lost miners come back one per MINER_REBUY_INTERVAL, and none while the base is threatened or after its loss.
            bool isMinerRebuy = miningFacilityCount < _peakMiningCount;
            bool minerAllowed = _enemyStructurePlacementService.CanPlaceMiner &&
                                (!isMinerRebuy || BattleClock.CurrentElapsed >= _nextMinerRebuyTime);
            _peakMiningCount = Math.Max(_peakMiningCount, miningFacilityCount);
            bool hasMiningOption = canPlaceStructure && hasMiningSelection && minerAllowed;
            bool canBuildMining = hasMiningOption && IsAffordable(mining.Value);
            // A small fleet takes the cheapest platform: a big one would cost it the ships of its opening.
            bool hasDefenseSelection = TrySelectDefense(shipCount < SAVING_MIN_SHIPS,
                out KeyValuePair<DefendPlatformType, FactionData> defense);

            KeyValuePair<SquadronType, FactionData> squadron = default;
            KeyValuePair<ShipType, FactionData> ship = default;
            _stationPressure.DefenderArrivalSeconds = _stationPressure.HasPressure
                ? GetQuickestDefenderArrival() : float.PositiveInfinity;
            bool critical = _stationPressure.IsStationCritical;
            bool canOrderShips = CanOrderAnyShip();
            // A threatened station saves for the best ship that still arrives in time. It never falls back to
            // level-1 corvettes: buying the cheapest hull every few seconds leaves no money for a ship that matters.
            bool hasDefenseShip = critical && canOrderShips && TrySelectShip(shipCount, out ship);
            bool hasSquadronRoom = HasSquadronRoom(shipCount);
            bool needsCaptureWings = !critical && CountReservedSquadrons() + CountHangarSquadrons() <
                GetCaptureWingTarget();
            // A small or losing fleet buys what fights now; only a strong fleet may save for a unit it cannot pay.
            bool buyNow = _stationPressure.HasPressure || !CanSaveForUnit(shipCount);
            UnitTypeId counterId = default;
            FactionData counterData = null;
            bool hasCounter = !hasDefenseShip && !needsCaptureWings && TrySelectCounterUnit(
                !hasSquadronRoom || !critical && canOrderShips && _decisionModel.NeedsMinimumFleet(shipCount),
                hasSquadronRoom, buyNow, out counterId, out counterData);
            bool hasSquadronFallback = !hasCounter && (needsCaptureWings || !critical && !canOrderShips) &&
                                       TrySelectSquadron(shipCount, out squadron, needsCaptureWings) &&
                                       IsAffordable(squadron.Value);
            // Under station pressure money buys what can fight now; saving for a capital loses the station.
            bool hasShip = hasDefenseShip ||
                           !critical && !hasCounter && !hasSquadronFallback && canOrderShips && TrySelectShip(shipCount,
                               out ship, requireAffordable: buyNow);
            bool hasShipOption = hasCounter || hasShip || hasSquadronFallback;
            FactionData shipChoice = hasCounter ? counterData : hasShip ? ship.Value : squadron.Value;
            DefenseReserve = hasShipOption ? shipChoice.Price : 0f;
            bool canBuildShip = hasShipOption && IsAffordable(shipChoice) && FitsRemainingCapacity(shipChoice);
            // A fleet at the unit limit cannot spend on ships, so held zones take further platforms.
            bool shipBlockedByCapacity = hasShipOption && IsAffordable(shipChoice) && !canBuildShip;
            // Platforms never outbid the fleet: while a ship can be bought, only the first platform of the station ring
            // or of a held zone is placed, and none while a team station is under pressure.
            bool platformsBlockedByPressure = canBuildShip && _stateProvider.HasTeamStationPressure;
            bool firstPlatformsOnly = hasShipOption && !shipBlockedByCapacity;
            bool canBuildDefense = BattleClock.CurrentElapsed >= _nextDefenseBuildTime &&
                !platformsBlockedByPressure && hasDefenseSelection && IsAffordable(defense.Value) &&
                _enemyStructurePlacementService.TryGetDefensePosition(shipBlockedByCapacity, firstPlatformsOnly, out _);
            UnitTypeId combatChoice = hasCounter ? counterId : hasShip
                ? UnitTypeId.Ship(ship.Key) : UnitTypeId.Squadron(squadron.Key);
            if (_stationPressure.IsStationCritical && !hasShipOption)
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "production-wait",
                    FormattableString.Invariant($"critical=True;etaSeconds=Infinity;timeToLoss={_stationPressure.EstimatedTimeToLossSeconds:F1};") +
                    "reason=no affordable combat unit can arrive before station loss");
            }
            _capacityBlocked = shipBlockedByCapacity;
            bool choiceFits = hasShipOption && FitsRemainingCapacity(shipChoice);
            // A big ship that does not fit yet may still be saved for: it is built for the fleet after the next losses.
            bool savingForShip = !critical && hasShipOption && !canBuildShip;
            if (savingForShip)
                TrackSaving(combatChoice.ToString());
            float savingForSeconds = GetSavingForSeconds();
            if (hasShipOption && !canBuildShip)
            {
                string waitReason = !IsAffordable(shipChoice) ? "saving" : "capacity";
                // Capacity rows name the best ship that fits and can be paid now ("none" when nothing fits).
                string bestFit = !choiceFits && canOrderShips &&
                    TrySelectShip(shipCount, out KeyValuePair<ShipType, FactionData> fitting, requireAffordable: true)
                    ? fitting.Key.ToString() : "none";
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "production-wait",
                    FormattableString.Invariant($"unit={combatChoice};capacity={_unitLimitModel.CurrentUnitCapacity}/{_reinforcementData.MaxUnitCapacity};required={shipChoice.UnitCapacity};cost={shipChoice.Price};money={_economyModel.Money:F1};critical={_stationPressure.IsStationCritical};criticalCause={_stationPressure.CriticalCause};etaSeconds={GetArrivalSeconds(combatChoice, shipChoice):F1};timeToLoss={_stationPressure.EstimatedTimeToLossSeconds:F1};saving={_savingUnit};savingFor={savingForSeconds:F1};fits={choiceFits};bestFit={bestFit};reason={waitReason}"));
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
            bool canResearchPriorityIncome = canResearchIncome &&
                incomeResearch is ResearchType.IncreasedProduction or ResearchType.IncreasedStationIncome;
            bool canResearchCombat = TrySelectResearch(false, out ResearchType combatResearch,
                                         out ResearchTierData combatTier) &&
                                     IsAffordable(combatTier.FactionData) &&
                                     BattleClock.CurrentElapsed >= _nextCombatResearchTime;
            if (savingForShip)
            {
                float nonCompetingCap = shipChoice.Price * NON_COMPETING_RESEARCH_SHARE;
                if (canResearchIncome && !canResearchPriorityIncome && incomeTier.FactionData.Price > nonCompetingCap)
                    canResearchIncome = false;
                if (canResearchCombat && combatTier.FactionData.Price > nonCompetingCap)
                    canResearchCombat = false;
            }
            int miningFacilityTarget = GetMiningFacilityTarget();

            float cheapestCapital = float.PositiveInfinity;
            if (_factionModel.CurrentLevel >= 4)
                foreach (var option in _factionModel.ShipFactionData)
                    if (_factionModel.IsAssaultCapital(option.Key, _profileCatalog.Get(UnitTypeId.Ship(option.Key)).ShipClass) &&
                        IsAvailable(option.Value))
                        cheapestCapital = Mathf.Min(cheapestCapital, option.Value.Price);
            _factionModel.HasAssaultSurplus = _economyModel.Money >=
                EnemyFactionModel.MINIMUM_ASSAULT_CAPITALS * cheapestCapital + (levelData?.Price ?? 0f) + (incomeTier?.FactionData.Price ?? 0f);

            if (TryBuySecondStation(critical)) return;
            bool secondStationLevelDue = IsSecondStationLevelDue(critical, out FactionData secondLevelData);
            // From level 2 the second station's price is held before ships, like a due station level, so buying what
            // fights now cannot starve it; after the main station falls the second station's upgrades are held the same way.
            float secondStationReserve = GetSecondStationReserve(critical);
            if (secondStationReserve <= 0f && secondStationLevelDue && IsSecondStationTheBase())
                secondStationReserve = secondLevelData.Price;

            if (!canResearchPriorityIncome && !_stationPressure.IsStationCritical && TryBuyScout(shipCount,
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
                canBuildDefense ? defensePlatformCount + 1 : defensePlatformCount,
                hasMiningOption,
                hasShipOption,
                canBuildShip,
                canBuildMining,
                canBuildDefense,
                hasLevelUpOption,
                canLevelUp,
                canResearchIncome,
                canResearchCombat,
                // Before the first corvette is allowed, missing capture wings are the opening fleet.
                canOrderShips || needsCaptureWings && hasSquadronFallback,
                _stationPressure.IsStationCritical,
                canResearchPriorityIncome);
            EnemyProductionCategory category = _decisionModel.Evaluate(snapshot);
            // A second-station upgrade follows miners, priority income research and a due station level, and comes
            // before combat research, platforms and ships.
            if (secondStationLevelDue && IsAffordable(secondLevelData) &&
                category is not (EnemyProductionCategory.Mining or EnemyProductionCategory.Level) &&
                !(category == EnemyProductionCategory.Research && canResearchIncome) &&
                !(category == EnemyProductionCategory.None && _decisionModel.IsLevelDue(snapshot)) &&
                TryBuySecondStationLevel(secondLevelData, critical))
                return;
            if (!_purchaseProcessor.IsUpgradingStation && _purchaseProcessor.HasActiveProduction &&
                !critical && category is (EnemyProductionCategory.Ship or EnemyProductionCategory.Defense) &&
                (PriorityConstructionPending || _decisionModel.IsLevelDue(snapshot) ||
                 incomeTier != null && !canResearchIncome))
                return;
            if (category == EnemyProductionCategory.None && _decisionModel.IsLevelDue(snapshot))
            {
                DefenseReserve = levelData.Price;
                TrackSaving($"Level{_factionModel.CurrentLevel + 1}");
            }
            if (secondStationReserve > 0f && !_decisionModel.NeedsMinimumFleet(shipCount) &&
                (category is EnemyProductionCategory.Ship or EnemyProductionCategory.Defense ||
                 category == EnemyProductionCategory.Research && !canResearchIncome))
            {
                DefenseReserve = secondStationReserve;
                TrackSaving(secondStationLevelDue ? $"SecondStationLevel{_secondStation.CurrentLevel + 1}" : "SecondStation");
                return;
            }
            if (category == EnemyProductionCategory.Ship && _factionModel.CurrentLevel >= 4 &&
                _stateProvider.CurrentState is (EnemyStrategicState.Consolidate or EnemyStrategicState.AssaultBase or
                    EnemyStrategicState.RaidStructures) &&
                TrySelectShip(shipCount, out var capital, requireAffordable: true, capitalOnly: true))
            {
                ship = capital;
                hasCounter = false;
                hasSquadronFallback = false;
                combatChoice = UnitTypeId.Ship(ship.Key);
            }

            bool buildSquadron = !_stationPressure.IsStationCritical && !hasCounter &&
                                 category == EnemyProductionCategory.Ship &&
                                 (hasSquadronFallback ||
                                  !_decisionModel.NeedsMinimumFleet(shipCount) &&
                                  (_factionModel.CurrentLevel < 4 || !combatChoice.IsShip ||
                                   !_factionModel.IsAssaultCapital(combatChoice.ShipType, _profileCatalog.Get(combatChoice).ShipClass)) &&
                                  TrySelectSquadron(shipCount, out squadron, needsCaptureWings) &&
                                  IsAffordable(squadron.Value));
            // Satellites and mines on captured sites are paid before any ship and any extra wing; only missing capture
            // wings keep being bought meanwhile.
            if (!critical && PriorityConstructionPending && category == EnemyProductionCategory.Ship &&
                !(buildSquadron && needsCaptureWings) && !(hasCounter && !counterId.IsShip))
                return;
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

            // Miners still missing, a due station level and the second station's reserve come before an interceptor.
            float mandatoryCosts = secondStationReserve +
                (secondStationLevelDue ? secondLevelData.Price : 0f) +
                (_decisionModel.IsLevelDue(snapshot) ? levelData.Price : 0f) +
                (hasMiningSelection ? Math.Max(0, miningFacilityTarget - miningFacilityCount) * mining.Value.Price : 0f);
            if (request == null)
            {
                if (needsCaptureWings) TryBuyParallelInterceptor(shipCount, mandatoryCosts);
                return;
            }

            DefenseReserve = request.FactionData.Price;

            Debug.Log(
                $"[EnemyAI:Production] BattleTime={BattleClock.CurrentElapsed:F1}, Player={_owner.Id}:{_owner.Faction}, State={_stateProvider.CurrentState}, " +
                $"FactionLevel={_factionModel.CurrentLevel}, Category={category}, Counter={hasCounter}, " +
                $"Unit={request.Id}, Mining={miningFacilityCount}/{miningFacilityTarget}, " +
                $"Defense={defensePlatformCount}/{snapshot.DefensePlatformTarget}, " +
                $"Cost={request.FactionData.Price}, " +
                $"Money={_economyModel.Money}");
            EnemyAiTelemetry.RecordPurchase(_owner.Id, _owner.Faction, _stateProvider.CurrentState, category,
                hasCounter, request.Id, request.FactionData.Price, _economyModel.Money,
                _stationPressure.IsStationCritical,
                category == EnemyProductionCategory.Ship ? GetArrivalSeconds(
                    buildSquadron ? UnitTypeId.Squadron(squadron.Key) : combatChoice,
                    request.FactionData) : 0f,
                _stationPressure.EstimatedTimeToLossSeconds,
                _savingUnit, GetSavingForSeconds());
            if (category is EnemyProductionCategory.Ship or EnemyProductionCategory.Level)
            {
                _savingUnit = "";
                _savingSince = -1f;
            }
            float moneyBefore = _economyModel.Money;
            _purchaseProcessor.Purchase(request);
            if (category == EnemyProductionCategory.Ship && request is ShipUnitRequest)
                TryBuySurplusShip(moneyBefore, request.FactionData);
            if (category == EnemyProductionCategory.Defense)
                _nextDefenseBuildTime = BattleClock.CurrentElapsed + DEFENSE_BUILD_INTERVAL;
            if (category == EnemyProductionCategory.Mining && isMinerRebuy)
                _nextMinerRebuyTime = BattleClock.CurrentElapsed + MINER_REBUY_INTERVAL;
            if (category == EnemyProductionCategory.Research && !canResearchIncome)
                _nextCombatResearchTime = BattleClock.CurrentElapsed + COMBAT_RESEARCH_INTERVAL;
            if (needsCaptureWings) TryBuyParallelInterceptor(shipCount, mandatoryCosts);
        }

        /// <summary>From level 2, once its price is paid and no mandatory satellite or mine waits.</summary>
        private bool TryBuySecondStation(bool critical)
        {
            float now = BattleClock.CurrentElapsed;
            if (now < _nextStationPlacementCheck || _factionModel.CurrentLevel < 2 ||
                _factionModel.AdditionalStationsBuilt != 0 || _productionAccess.ProductionHost != _owner.Id ||
                !_productionAccess.HasHostBase || !critical && PriorityConstructionPending)
                return false;
            FactionData data = _factionModel.SecondStationData;
            if (_economyModel.Money < data.Price ||
                !CanReserve<SecondStationUnitRequest>(SecondStationUnitRequest.REQUEST_ID, data)) return false;
            _nextStationPlacementCheck = now + STATION_PLACEMENT_RETRY_SECONDS;
            if (!_enemyStructurePlacementService.TryGetSecondStationPosition(out _)) return false;

            UnitRequest request = _requestFactory.ConstructSecondStationRequest(data);
            _purchaseProcessor.Purchase(request);
            EnemyAiTelemetry.RecordPurchase(_owner.Id, _owner.Faction, _stateProvider.CurrentState,
                EnemyProductionCategory.SecondStation, false, request.Id, data.Price, _economyModel.Money,
                critical, 0f, _stationPressure.EstimatedTimeToLossSeconds);
            return true;
        }

        /// <summary>
        /// Price of the second station while it is wanted (level 2+, none built, own base standing, nothing mandatory
        /// pending) but not yet affordable; 0 otherwise.
        /// </summary>
        private float GetSecondStationReserve(bool critical)
        {
            if (critical || _factionModel.CurrentLevel < 2 || _factionModel.AdditionalStationsBuilt != 0 ||
                _productionAccess.ProductionHost != _owner.Id || !_productionAccess.HasHostBase ||
                PriorityConstructionPending)
                return 0f;
            FactionData data = _factionModel.SecondStationData;
            return IsAffordable(data) ||
                   !CanReserve<SecondStationUnitRequest>(SecondStationUnitRequest.REQUEST_ID, data) ? 0f : data.Price;
        }

        /// <summary>
        /// The second station stands below the main station's level and no upgrade of it builds. While the main station
        /// stands, a critical station or a pending satellite or mine comes first.
        /// </summary>
        private bool IsSecondStationLevelDue(bool critical, out FactionData data)
        {
            data = null;
            if (!_secondStation.IsBuilt || _secondStation.CurrentLevel >= _factionModel.CurrentLevel ||
                _productionAccess.ProductionHost != _owner.Id || _purchaseProcessor.IsUpgradingSecondStation)
                return false;
            if ((critical || PriorityConstructionPending) && !IsSecondStationTheBase()) return false;
            data = _factionModel.GetSecondStationLevelFactionData(_secondStation.CurrentLevel);
            return data != null;
        }

        private bool TryBuySecondStationLevel(FactionData data, bool critical)
        {
            UnitRequest request = _requestFactory.ConstructSecondStationLevelRequest(data, _secondStation.CurrentLevel);
            if (!_purchaseProcessor.CanQueue(request.Id, data.MaxCount)) return false;
            _purchaseProcessor.Purchase(request);
            EnemyAiTelemetry.RecordPurchase(_owner.Id, _owner.Faction, _stateProvider.CurrentState,
                EnemyProductionCategory.SecondStationLevel, false, request.Id, data.Price, _economyModel.Money,
                critical, 0f, _stationPressure.EstimatedTimeToLossSeconds);
            return true;
        }

        /// <summary>The main station fell and the second station is the host's production base.</summary>
        private bool IsSecondStationTheBase() =>
            _secondStation.IsBuilt && _productionAccess.TryGetProductionBase(out IEntity productionBase) &&
            productionBase.Owner == _owner.Id && productionBase.TryGetFacade(out ISecondStationFacade _);

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
            bool requireAffordable,
            out UnitTypeId selectedId,
            out FactionData selectedData)
        {
            selectedId = default;
            selectedData = null;
            PlayerId self = _owner.Id;
            // Counters answer what the team has seen of the enemy, not the whole hidden map; an armada that threatens a
            // team asset and outmatches the fleet is answered alone, so new ships are bought to beat it.
            if (!_stateProvider.TryCopyOutmatchingArmada(_hostileForce))
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
                    IsAvailable(option.Value) && IsShipClassAllowed(unitId) &&
                    IsShipTierAllowed(option.Value, unitId), hasSquadronRoom, requireAffordable);
            }

            // Bombers fight beside ships: with no own ship to go with they only wait at a hold and die there.
            // A critical station is the exception: its defence wings fly to the station, not to a hold.
            bool hasOwnShips = HasLiveShips() || _stationPressure.IsStationCritical;
            foreach (KeyValuePair<SquadronType, FactionData> option in _factionModel.SquadronFactionData)
            {
                UnitTypeId squadronId = UnitTypeId.Squadron(option.Key);
                bool bomber = _profileCatalog.Get(squadronId).ShipClass == ShipClass.Bomber;
                AddCounterOption<SquadronUnitRequest>(squadronId, option.Key.ToString(),
                    option.Value, !shipsOnly && hasSquadronRoom && (hasOwnShips || !bomber), hasSquadronRoom,
                    requireAffordable);
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
            bool hasSquadronRoom,
            bool requireAffordable)
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

            if (requireAffordable && !IsAffordable(data)) return;
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
            out KeyValuePair<ShipType, FactionData> selected,
            bool requireAffordable = false,
            bool capitalOnly = false)
        {
            selected = default;
            bool found = false;
            float bestPriority = float.MaxValue;
            _criticalShips.Clear();
            KeyValuePair<ShipType, FactionData> bestPerSlot = default;
            float bestSlotValue = 0f;
            int dearest = 0;
            foreach (KeyValuePair<ShipType, FactionData> option
                     in _factionModel.ShipFactionData)
            {
                // Affordability is checked after selection so the AI saves for higher-tier ships.
                UnitTypeId unitType = UnitTypeId.Ship(option.Key);
                if (!IsAvailable(option.Value) || !IsShipClassAllowed(unitType) ||
                    !IsShipTierAllowed(option.Value, unitType) ||
                    !CanReserve<ShipUnitRequest>(
                        option.Key.ToString(),
                        option.Value, true))
                {
                    continue;
                }

                if (requireAffordable && (!IsAffordable(option.Value) || !FitsRemainingCapacity(option.Value)))
                    continue;
                if (capitalOnly && !_factionModel.IsAssaultCapital(option.Key, _profileCatalog.Get(unitType).ShipClass))
                    continue;

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
                // Saving for a ship that does not fit the free capacity stays allowed (dreadnoughts, battlecruisers
                // are built for the future), at a lower priority than ships that fit (W34, user 2026-11-22).
                if (!FitsRemainingCapacity(option.Value)) priority *= NO_FIT_SAVING_PRIORITY_SCALE;
                if (_stationPressure.IsStationCritical)
                {
                    if (combatValuePerCredit <= 0f) continue;
                    float arrival = GetArrivalSeconds(unitType, option.Value);
                    if (float.IsPositiveInfinity(arrival) || !(arrival <= _stationPressure.EstimatedTimeToLossSeconds))
                        continue;
                    _criticalShips.Add((option, arrival, combatValuePerCredit,
                        _profileCatalog.Get(unitType).GetCombatValue() / Math.Max(1, option.Value.UnitCapacity)));
                    continue;
                }
                float perSlot = _profileCatalog.Get(unitType).GetCombatValue() / Math.Max(1, option.Value.UnitCapacity);
                dearest = Math.Max(dearest, option.Value.Price);
                if (perSlot > bestSlotValue)
                {
                    bestPerSlot = option;
                    bestSlotValue = perSlot;
                }
                if (!found || priority < bestPriority ||
                    priority == bestPriority && option.Value.Price < selected.Value.Price)
                {
                    selected = option;
                    bestPriority = priority;
                    found = true;
                }
            }

            if (_stationPressure.IsStationCritical) return TrySelectCriticalShip(out selected);
            // At the unit cap with money for the dearest candidate, a freed slot goes to the best value per slot (W34 b).
            if (found && _capacityBlocked && bestSlotValue > 0f && _economyModel.Money >= dearest)
                selected = bestPerSlot;
            return found;
        }

        /// <summary>
        /// Among ships that arrive before the station falls: those inside the tier span when any arrives in time.
        /// With money for the dearest of them, the best combat value per capacity slot wins (the cap, not money,
        /// limits the fleet); otherwise arrival time per combat value per credit, as before (W35).
        /// </summary>
        private bool TrySelectCriticalShip(out KeyValuePair<ShipType, FactionData> selected)
        {
            selected = default;
            if (_criticalShips.Count == 0) return false;
            int floor = GetShipTierFloor();
            bool anyInSpan = false;
            foreach (var candidate in _criticalShips)
                anyInSpan |= candidate.Option.Value.AvailableLevel >= floor;
            int dearest = 0;
            foreach (var candidate in _criticalShips)
                if (!anyInSpan || candidate.Option.Value.AvailableLevel >= floor)
                    dearest = Math.Max(dearest, candidate.Option.Value.Price);
            bool bySlot = _economyModel.Money >= dearest;
            float best = float.MaxValue;
            bool found = false;
            foreach (var candidate in _criticalShips)
            {
                if (anyInSpan && candidate.Option.Value.AvailableLevel < floor) continue;
                float priority = bySlot ? -candidate.PerSlot : Math.Max(1f, candidate.Arrival) / candidate.PerCredit;
                if (found && (priority > best || priority == best &&
                        candidate.Option.Value.Price >= selected.Value.Price)) continue;
                selected = candidate.Option;
                best = priority;
                found = true;
            }
            _criticalShips.Clear();
            return found;
        }

        /// <summary>Arrival time of the quickest affordable ship that fits the free capacity (W36 critical rule).</summary>
        private float GetQuickestDefenderArrival()
        {
            float quickest = float.PositiveInfinity;
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
            {
                UnitTypeId unitType = UnitTypeId.Ship(option.Key);
                if (!IsAvailable(option.Value) || !IsAffordable(option.Value) || !FitsRemainingCapacity(option.Value) ||
                    !IsShipClassAllowed(unitType) || _profileCatalog.Get(unitType).GetCombatValue() <= 0f) continue;
                quickest = Math.Min(quickest, GetArrivalSeconds(unitType, option.Value));
            }
            return quickest;
        }

        /// <summary>Queue wait + build time, plus travel to the station threat when one is known.</summary>
        private float GetArrivalSeconds(UnitTypeId unitTypeId, FactionData data)
        {
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

        /// <summary>
        /// True while wings number fewer than one per <see cref="SHIPS_PER_SQUADRON"/> ships. Before the first ship is
        /// bought the wings are the whole fleet and stop at the opening cap (<see cref="EnemyWingPricing"/>: 6 cheap,
        /// 4 otherwise), so the opening money reaches mines and satellites.
        /// </summary>
        /// <remarks>Station and carrier hangar wings count toward the cap so hangars are no way around it.</remarks>
        private bool HasSquadronRoom(int shipCount)
        {
            int wings = CountReservedSquadrons() + CountHangarSquadrons();
            if (_stationPressure.IsStationCritical) return wings < MINIMUM_CAPTURE_WINGS;
            if (shipCount == 0) return wings < GetOpeningWingCap();
            return wings < Math.Max(GetCaptureWingTarget(), shipCount / SHIPS_PER_SQUADRON);
        }

        private int GetOpeningWingCap() =>
            EnemyWingPricing.HasCheapFighterWing(_factionModel.SquadronFactionData, _profileCatalog)
                ? EnemyWingPricing.CHEAP_OPENING_WING_CAP : EnemyWingPricing.OPENING_WING_CAP;

        // Big maps have more expansion zones than early ships: one capture wing per eligible zone plus one.
        private int GetCaptureWingTarget() =>
            Math.Max(MINIMUM_CAPTURE_WINGS, _stateProvider.EligibleExpansionZoneCount + 1);

        /// <summary>
        /// Saving for a unit the AI cannot pay yet is allowed only for a fleet of at least
        /// <see cref="SAVING_MIN_SHIPS"/> ships that beats the known hostile force, outside recovery mode.
        /// </summary>
        private bool CanSaveForUnit(int shipCount)
        {
            if (shipCount < SAVING_MIN_SHIPS || _enemyStructurePlacementService.IsRecovering) return false;
            PlayerId self = _owner.Id;
            _forceBuilder.BuildKnown(_hostileForce, _intel, Time.time, sighting => true);
            _forceBuilder.Build(_ownForce, entity => entity.Owner == self, _liveUnitCounts);
            return CombatMatchup.Advantage(_ownForce, _hostileForce) >= SAVING_MIN_ADVANTAGE;
        }

        /// <summary>
        /// With money at <see cref="SURPLUS_SHIP_FACTOR"/> times the price of the ship just bought, the best ship
        /// affordable with what is left is bought in the same cycle. From level 2: level-1 corvettes stay one at a time.
        /// </summary>
        private void TryBuySurplusShip(float moneyBefore, FactionData bought)
        {
            if (_factionModel.CurrentLevel < 2 || moneyBefore < SURPLUS_SHIP_FACTOR * bought.Price ||
                _stationPressure.IsStationCritical ||
                !CanOrderAnyShip() ||
                !TrySelectShip(CountReservedShips(), out KeyValuePair<ShipType, FactionData> extra,
                    requireAffordable: true))
                return;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "surplusShip",
                FormattableString.Invariant($"unit={extra.Key};cost={extra.Value.Price};money={_economyModel.Money:F1}"));
            UnitRequest request = _requestFactory.ConstructUnitRequest(extra.Value, extra.Key);
            RecordExtraPurchase(request);
            _purchaseProcessor.Purchase(request);
        }

        private void TrackSaving(string unit)
        {
            if (_savingUnit == unit) return;
            _savingUnit = unit;
            _savingSince = BattleClock.CurrentElapsed;
        }

        private float GetSavingForSeconds() =>
            _savingSince >= 0f ? Mathf.Max(0f, BattleClock.CurrentElapsed - _savingSince) : 0f;

        private bool IsLevel1Corvette(FactionData data, UnitTypeId id) =>
            data.AvailableLevel <= 1 && _profileCatalog.Get(id).ShipClass == ShipClass.Corvette;

        private int CountReservedLevel1Corvettes()
        {
            int count = 0;
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
            {
                if (IsLevel1Corvette(option.Value, UnitTypeId.Ship(option.Key)))
                    count += _unitLimitModel.GetReservedCount<ShipUnitRequest>(option.Key.ToString());
            }
            return count;
        }

        /// <summary>
        /// Level-1 corvettes answer enemy wings in the opening: none in the first
        /// <see cref="OPENING_NO_CORVETTE_SECONDS"/>, at most <see cref="MAX_LEVEL1_CORVETTES"/>, and never once
        /// level 2 is reached, station emergency included. Other ships are bought only from the station's level and
        /// <see cref="SHIP_TIER_SPAN"/> below it, so a high-level station stops filling its queue with early frigates;
        /// a station about to fall takes any ship that arrives in time. An earlier ship stays on the list while it
        /// fights better per credit than every ship of the span (a faction whose best hulls unlock early).
        /// </summary>
        private bool IsShipTierAllowed(FactionData data, UnitTypeId id)
        {
            if (IsLevel1Corvette(data, id))
                return _factionModel.CurrentLevel == 1 && BattleClock.CurrentElapsed >= OPENING_NO_CORVETTE_SECONDS &&
                    CountReservedLevel1Corvettes() < MAX_LEVEL1_CORVETTES;
            if (_stationPressure.IsStationCritical) return true;
            int floor = GetShipTierFloor();
            return data.AvailableLevel >= floor || GetValuePerCredit(id, data) > GetBestSpanValuePerCredit(floor);
        }

        private float GetValuePerCredit(UnitTypeId id, FactionData data) =>
            _profileCatalog.Get(id).GetCombatValue() / Math.Max(1, data.Price);

        private float GetBestSpanValuePerCredit(int floor)
        {
            float best = 0f;
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
                if (option.Value.AvailableLevel >= floor && IsAvailable(option.Value))
                    best = Math.Max(best, GetValuePerCredit(UnitTypeId.Ship(option.Key), option.Value));
            return best;
        }

        /// <summary>The highest level floor within <see cref="SHIP_TIER_SPAN"/> that still leaves a ship type to buy.</summary>
        private int GetShipTierFloor()
        {
            int level = _factionModel.CurrentLevel;
            for (int floor = level - SHIP_TIER_SPAN; floor > 1; floor--)
                foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
                    if (option.Value.AvailableLevel >= floor && option.Value.AvailableLevel <= level)
                        return floor;
            return 1;
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

        private int CountReservedBombers()
        {
            int count = 0;
            foreach (KeyValuePair<SquadronType, FactionData> option in _factionModel.SquadronFactionData)
                if (_profileCatalog.Get(UnitTypeId.Squadron(option.Key)).ShipClass == ShipClass.Bomber)
                    count += _unitLimitModel.GetReservedCount<SquadronUnitRequest>(option.Key.ToString());
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
                UnitTypeId unitType = UnitTypeId.Ship(option.Key);
                if (IsAvailable(option.Value) && IsShipClassAllowed(unitType) &&
                    IsShipTierAllowed(option.Value, unitType) &&
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
                !TrySelectSquadron(shipCount, out KeyValuePair<SquadronType, FactionData> scout, captureWing: true) ||
                !IsAffordable(scout.Value))
            {
                return false;
            }

            UnitRequest request = _requestFactory.ConstructUnitRequest(scout.Value, scout.Key);
            Debug.Log(
                $"[EnemyAI:Production] BattleTime={BattleClock.CurrentElapsed:F1}, Player={_owner.Id}:{_owner.Faction}, Category=Scout, Unit={request.Id}, " +
                $"Cost={request.FactionData.Price}, Money={_economyModel.Money}");
            RecordExtraPurchase(request);
            _purchaseProcessor.Purchase(request);
            return true;
        }

        /// <summary>A purchase row for a unit bought outside the main choice (surplus ship, scout, patrol wing, interceptor).</summary>
        private void RecordExtraPurchase(UnitRequest request) =>
            EnemyAiTelemetry.RecordPurchase(_owner.Id, _owner.Faction, _stateProvider.CurrentState,
                EnemyProductionCategory.Ship, false, request.Id, request.FactionData.Price, _economyModel.Money,
                _stationPressure.IsStationCritical, 0f, _stationPressure.EstimatedTimeToLossSeconds,
                _savingUnit, GetSavingForSeconds());

        /// <summary>
        /// From level 2 the patrol is a fighter or interceptor wing. When no wing can be spared, the cheapest one is
        /// bought outside the wing cap; one order per <see cref="PATROL_WING_RETRY_SECONDS"/> while it builds.
        /// </summary>
        private bool TryBuyPatrolWing()
        {
            if (_stationPressure.HasPressure || !_squadrons.NeedsPatrolWing ||
                BattleClock.CurrentElapsed < _patrolWingRetryAt) return false;
            KeyValuePair<SquadronType, FactionData> cheapest = default;
            bool found = false;
            foreach (KeyValuePair<SquadronType, FactionData> option in _factionModel.SquadronFactionData)
            {
                ShipClass wingClass = _profileCatalog.Get(UnitTypeId.Squadron(option.Key)).ShipClass;
                if (wingClass != ShipClass.Fighter && wingClass != ShipClass.Interceptor ||
                    !IsAvailable(option.Value) || !CanReserve<SquadronUnitRequest>(option.Key.ToString(), option.Value) ||
                    found && option.Value.Price >= cheapest.Value.Price) continue;
                cheapest = option;
                found = true;
            }
            if (!found || !IsAffordable(cheapest.Value)) return false;
            _patrolWingRetryAt = BattleClock.CurrentElapsed + PATROL_WING_RETRY_SECONDS;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "patrolWingPurchase",
                $"unit={cheapest.Key};price={cheapest.Value.Price}");
            UnitRequest request = _requestFactory.ConstructUnitRequest(cheapest.Value, cheapest.Key);
            RecordExtraPurchase(request);
            _purchaseProcessor.Purchase(request);
            return true;
        }

        /// <summary>
        /// Picks a wing while <see cref="HasSquadronRoom"/>: the cheapest <see cref="ShipClass.Fighter"/> wing (the
        /// cheapest interceptor when the faction has no fighter yet). Outside the capture wings, bombers rotate in once
        /// a hostile mine is known to raid (at most <see cref="BOMBERS_PER_HOSTILE_MINE"/> per mine), and any bomber is
        /// the last choice. Interceptors come from <see cref="TryBuyParallelInterceptor"/>; elite fighters and extra
        /// interceptors only as counters. Hero wings are never picked here.
        /// </summary>
        private bool TrySelectSquadron(
            int shipCount,
            out KeyValuePair<SquadronType, FactionData> selected,
            bool captureWing = false)
        {
            selected = default;
            if (!HasSquadronRoom(shipCount))
            {
                return false;
            }

            bool hasFighter = TrySelectCheapestWing(ShipClass.Fighter, out KeyValuePair<SquadronType, FactionData> fighter) ||
                              TrySelectCheapestWing(ShipClass.Interceptor, out fighter);
            if (captureWing && hasFighter)
            {
                selected = fighter;
                return true;
            }

            bool found = false;
            bool selectedIsBomber = false;
            int selectedCount = 0;
            bool bombersWanted = _intel.Mines.Count > 0;
            bool bomberRoom = CountReservedBombers() < BOMBERS_PER_HOSTILE_MINE * _intel.Mines.Count;
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

                ShipClass wingClass = _profileCatalog.Get(UnitTypeId.Squadron(option.Key)).ShipClass;
                bool isCheapFighter = hasFighter && option.Key == fighter.Key;
                if (wingClass == ShipClass.HeroStrikecraft || wingClass == ShipClass.Bomber && bombersWanted && !bomberRoom ||
                    wingClass != ShipClass.Bomber && hasFighter && !isCheapFighter)
                    continue;
                bool isBomber = wingClass == ShipClass.Bomber && !bombersWanted;
                int count = _unitLimitModel.GetReservedCount<SquadronUnitRequest>(option.Key.ToString());
                if (found && (isBomber && !selectedIsBomber ||
                              isBomber == selectedIsBomber && (count > selectedCount ||
                                  count == selectedCount && option.Value.Price >= selected.Value.Price)))
                {
                    continue;
                }

                selected = option;
                selectedIsBomber = isBomber;
                selectedCount = count;
                found = true;
            }

            return found;
        }

        /// <summary>The cheapest available wing of <paramref name="wingClass"/> that can be queued now.</summary>
        private bool TrySelectCheapestWing(ShipClass wingClass, out KeyValuePair<SquadronType, FactionData> selected)
        {
            selected = default;
            bool found = false;
            foreach (KeyValuePair<SquadronType, FactionData> option in _factionModel.SquadronFactionData)
            {
                if (_profileCatalog.Get(UnitTypeId.Squadron(option.Key)).ShipClass != wingClass ||
                    !IsAvailable(option.Value) ||
                    !CanReserve<SquadronUnitRequest>(option.Key.ToString(), option.Value) ||
                    found && option.Value.Price >= selected.Value.Price) continue;
                selected = option;
                found = true;
            }
            return found;
        }

        /// <summary>
        /// While capture wings are missing, one interceptor wing builds beside them on its own pipeline when the money
        /// left after the mandatory bill (<paramref name="mandatoryCosts"/>) pays for it. Nothing is added while a
        /// satellite or mine on a captured site waits.
        /// </summary>
        private void TryBuyParallelInterceptor(int shipCount, float mandatoryCosts)
        {
            if (PriorityConstructionPending || _stationPressure.IsStationCritical || !HasSquadronRoom(shipCount) ||
                !TrySelectCheapestWing(ShipClass.Interceptor, out KeyValuePair<SquadronType, FactionData> interceptor) ||
                _purchaseProcessor.GetQueueRemainingTime(interceptor.Key.ToString()) > 0f ||
                _economyModel.Money - mandatoryCosts < interceptor.Value.Price)
                return;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "parallelInterceptor",
                FormattableString.Invariant(
                    $"unit={interceptor.Key};cost={interceptor.Value.Price};money={_economyModel.Money:F1};mandatory={mandatoryCosts:F1}"));
            UnitRequest request = _requestFactory.ConstructUnitRequest(interceptor.Value, interceptor.Key);
            RecordExtraPurchase(request);
            _purchaseProcessor.Purchase(request);
        }

        /// <summary>
        /// Prefers an affordable type with fewer existing copies, then the strongest platform within budget.
        /// With <paramref name="cheapestOnly"/> the cheapest buildable type is the only choice.
        /// </summary>
        private bool TrySelectDefense(
            bool cheapestOnly,
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

                if (cheapestOnly)
                {
                    if (!found || option.Value.Price < selected.Value.Price)
                    {
                        selected = option;
                        found = true;
                    }
                    continue;
                }

                bool isAffordable = IsAffordable(option.Value);
                bool isSelectedAffordable = found && IsAffordable(selected.Value);
                int copies = _unitLimitModel.GetReservedCount<DefendPlatformUnitRequest>(option.Key.ToString());
                int selectedCopies = found
                    ? _unitLimitModel.GetReservedCount<DefendPlatformUnitRequest>(selected.Key.ToString()) : int.MaxValue;
                bool isBetter = !found || (isAffordable
                    ? !isSelectedAffordable || copies < selectedCopies ||
                      copies == selectedCopies && option.Value.Price > selected.Value.Price
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

                int priority = IncomePriority(researchType);
                int selectedPriority = IncomePriority(selectedType);
                if (selectedTier == null || priority > selectedPriority ||
                    priority == selectedPriority && tier.FactionData.Price < selectedTier.FactionData.Price)
                {
                    selectedType = researchType;
                    selectedTier = tier;
                }
            }

            return selectedTier != null;
        }

        private static int IncomePriority(ResearchType type) => type switch
        {
            ResearchType.IncreasedStationIncome => 2,
            ResearchType.IncreasedProduction => 1,
            _ => 0
        };

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

        /// <remarks>
        /// A ship type with a build already waiting behind the one in production is not bought again (one in
        /// production, none waiting): the money goes to another type or to the unit being saved for.
        /// </remarks>
        /// <summary>The last force build counted at least one living own ship.</summary>
        private bool HasLiveShips()
        {
            foreach (KeyValuePair<UnitTypeId, int> pair in _liveUnitCounts)
                if (pair.Key.IsShip && pair.Value > 0) return true;
            return false;
        }

        private bool CanReserve<TRequest>(string requestId, FactionData data, bool ignoreUsedCapacity = false)
        {
            return _purchaseProcessor.CanQueue(requestId, data.MaxCount) &&
                (typeof(TRequest) != typeof(ShipUnitRequest) || !_purchaseProcessor.HasWaitingBuild(requestId)) &&
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
