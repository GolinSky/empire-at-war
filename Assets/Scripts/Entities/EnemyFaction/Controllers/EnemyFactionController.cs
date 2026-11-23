using System;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using EmpireAtWar.Services.Timing;
#endif
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using ShipEntity = EmpireAtWar.Ship.Ship;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.Entities.EnemyFaction.Controllers
{
   //todo: why we have here spawn logic
    public class EnemyFactionController : Controller<EnemyFactionModel>, IEnemyPurchaseProcessor, IEnemyStationCheat, IInitializable, ILateDisposable, IIncomeProvider,
        ITickable
    {
        private const float DEPLOYMENT_RETRY_INTERVAL = 1f;
        private const float DEPLOYMENT_TIER_B_SECONDS = 4f;
        private const float MAX_DEPLOYMENT_HOLD_SECONDS = 8f;
        private const float DEPLOYMENT_TIER_D_SECONDS = 12f;
        private const float DEPLOYMENT_EMERGENCY_SECONDS = 15f;
        private const float LONE_REINFORCEMENT_WAIT_SECONDS = 12f;
        private const int MINIMUM_DEFENSE_WAVE = 2;
        private const int MINIMUM_REINFORCEMENT_WAVE = 3;
        private const float ASSAULT_WAVE_INTERVAL = 30f;
        private const float STATION_PLACEMENT_RETRY_SECONDS = 10f;
        private const float STRUCTURE_PLACEMENT_RETRY_SECONDS = 5f;

        private readonly IEconomyProvider _economyProvider;
        private readonly IWallet _wallet;
        private readonly IEconomyModelObserver _economyModel;
        private readonly SpaceStationFactory _stationFactory;
        private readonly UnitCombatProfileCatalog _profiles;
        private readonly IShipSpawnPoints _shipSpawnPoints;
        private readonly IEnemyAiStateProvider _aiState;
        private float _stationTelemetryAt;
        private float _assaultWavePlanUntil;
        private bool _outerWingCountSent;
        private bool _hadStation;
        private readonly IEnemyStructurePlacementService _enemyStructurePlacementService;
        private readonly IProductionAccess _productionAccess;
        private readonly ISquadronLauncher _squadronLauncher;
        private readonly IEnemySquadronCommander _squadronCommander;
        private readonly IPlayerRegistry _playerRegistry;

        private readonly ShipFactory _shipFactory;
        private readonly EnemyUnitLimitModel _unitLimitModel;
        private readonly ReinforcementData _reinforcementData;
        private readonly PlayerSlot _owner;
        private readonly EnemyBuildQueue _builds = new EnemyBuildQueue();
        private readonly PlacementThreatSnapshot _defenseDeploymentThreats = new PlacementThreatSnapshot();
        private readonly Dictionary<ShipUnitRequest, (Vector3 Position, string Tier, bool Forced, bool Emergency)> _defenseLandings =
            new Dictionary<ShipUnitRequest, (Vector3, string, bool, bool)>();
        private readonly HashSet<long> _builtSquadrons = new HashSet<long>();
        // Placement flags each platform was bought with; read when its build completes (W50).
        private readonly Dictionary<UnitRequest, (bool AllowExtra, bool FirstOnly)> _defenseFlags =
            new Dictionary<UnitRequest, (bool, bool)>();
        private readonly HashSet<DefendPlatformEntity> _platforms = new HashSet<DefendPlatformEntity>();
        private readonly MiningFacilityFactory _miningFacilityFactory;
        private readonly DefendPlatformFactory _defendPlatformFactory;
        private readonly FactionResearchModel _research;
        private readonly SecondStationLevelModel _secondStation;
        private readonly IEnemyForwardSpawnAnchors _forwardAnchors;
        private readonly List<ForwardSpawnAnchor> _anchors = new List<ForwardSpawnAnchor>();
        private readonly List<Vector3> _anchorPositions = new List<Vector3>();
        // Ships placed this tick are anchors for the next ship before the world snapshot sees them.
        private readonly List<Vector3> _landedShips = new List<Vector3>();
        private readonly Comparison<UnitRequest> _smallestShipFirst;
        private readonly HashSet<string> _busyPipelines = new HashSet<string>();
        // D3: while set, the ship being built lands beside a group ship fighting on its way.
        private Vector3? _dropPoint;

        private bool _isInitialized;

        private PlayerId Owner => _owner.Id;
        public float Income => StationIncome.Get(Model.CurrentLevel, _research.StationIncomeMultiplier, _secondStation.IsBuilt);
        public bool HasPendingReinforcement => _builds.Count > 0;
        public bool PriorityConstructionPending { get; set; }
        public bool IsUpgradingStation => _builds.Builds.Any(build => build.Request is LevelUnitRequest);
        public bool IsUpgradingSecondStation =>
            _builds.Builds.Any(build => build.Request is SecondStationLevelUnitRequest);
        public bool HasActiveProduction => _builds.Builds.Any(build => build.TimeLeft > 0f);
        public IReadOnlyList<EnemyPendingBuild> PendingBuilds => _builds.Builds;
        public bool HasPlatformInProgress =>
            _builds.Builds.Any(build => build.Request is DefendPlatformUnitRequest) ||
            _platforms.Any(platform => platform.IsConstructing);
        /// <summary>W56: the own station (main or second) stands; without it an assault capital lands only on the body.</summary>
        private bool HasOwnLivingStation => _productionAccess.ProductionHost == Owner && _productionAccess.HasHostBase;

        public bool CanQueue(string requestId, int maxCount) =>
            (requestId != SecondStationUnitRequest.REQUEST_ID ||
             Model.AdditionalStationsBuilt == 0 && Model.CurrentLevel >= 2 &&
             _productionAccess.ProductionHost == Owner && _productionAccess.HasHostBase) &&
            _builds.CanAdd(requestId, maxCount);
        public float GetQueueRemainingTime(string requestId) => _builds.GetRemainingTime(requestId);
        public bool HasWaitingBuild(string requestId) => _builds.CountUnfinished(requestId) > 1;

        public void ReserveExternalPipeline(string requestId) => _builds.ReserveExternalPipeline(requestId);

        public void ReleaseExternalPipeline(string requestId) => _builds.ReleaseExternalPipeline(requestId);
        /// <summary>Living squadrons the AI paid for; hangar squadrons are free and never listed.</summary>
        public IEnumerable<long> BuiltSquadrons => _builtSquadrons;

        public EnemyFactionController(
            IEconomyProvider economyProvider,
            IWallet wallet,
            IShipSpawnPoints shipSpawnPoints,
            IEnemyStructurePlacementService enemyStructurePlacementService,
            IProductionAccess productionAccess,
            ISquadronLauncher squadronLauncher,
            IEnemySquadronCommander squadronCommander,
            IPlayerRegistry playerRegistry,
            EnemyFactionModel model,
            ShipFactory shipFactory,
            MiningFacilityFactory miningFacilityFactory,
            DefendPlatformFactory defendPlatformFactory,
            FactionResearchModel research,
            EnemyUnitLimitModel unitLimitModel,
            ReinforcementData reinforcementData,
            PlayerSlot owner,
            IEnemyAiStateProvider aiState,
            SpaceStationFactory stationFactory,
            UnitCombatProfileCatalog profiles,
            SecondStationLevelModel secondStation,
            IEnemyForwardSpawnAnchors forwardAnchors,
            IEconomyModelObserver economyModel) : base(model)
        {
            _economyModel = economyModel;
            _secondStation = secondStation;
            _forwardAnchors = forwardAnchors;
            _smallestShipFirst = CompareHullSize;
            _owner = owner;
            _aiState = aiState;
            _stationFactory = stationFactory;
            _profiles = profiles;
            _playerRegistry = playerRegistry;
            _shipFactory = shipFactory;
            _miningFacilityFactory = miningFacilityFactory;
            _defendPlatformFactory = defendPlatformFactory;
            _research = research;
            _economyProvider = economyProvider;
            _wallet = wallet;
            _shipSpawnPoints = shipSpawnPoints;
            _unitLimitModel = unitLimitModel;
            _reinforcementData = reinforcementData;
            _enemyStructurePlacementService = enemyStructurePlacementService;
            _productionAccess = productionAccess;
            _squadronLauncher = squadronLauncher;
            _squadronCommander = squadronCommander;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _unitLimitModel.Reset();
            _enemyStructurePlacementService.Reset();
            _economyProvider.AddProvider(this);
            Model.OnLevelUpgraded += RecalculateIncome;
            _secondStation.BuiltChanged += HandleSecondStationChanged;
            _playerRegistry.RegisterAiStationCheat(Owner, this);
            _isInitialized = true;
        }

        public void LateDispose()
        {
            CancelPendingBuilds();

            if (!_isInitialized)
            {
                return;
            }

            _economyProvider.RemoveProvider(this);
            Model.OnLevelUpgraded -= RecalculateIncome;
            _secondStation.BuiltChanged -= HandleSecondStationChanged;
            _playerRegistry.UnregisterAiStationCheat(Owner);
            _isInitialized = false;
        }

        public bool ForceUpgradeStation()
        {
            if (_productionAccess.ProductionHost != Owner ||
                Model.GetCurrentLevelFactionData() == null || !_productionAccess.HasHostBase) return false;

            // Refund a building upgrade so its later completion cannot exceed the level limit.
            foreach (EnemyPendingBuild upgrade in _builds.Builds.Where(build => build.Request is LevelUnitRequest))
                _wallet.Refund(upgrade.Request);
            _builds.RemoveAll(request => request is LevelUnitRequest);
            Model.CompleteLevelUpgrade();
            return true;
        }

        public void Purchase(UnitRequest unitRequest)
        {
            if (unitRequest is SecondStationLevelUnitRequest secondLevel && !CanUpgradeSecondStation(secondLevel) ||
                !CanQueue(unitRequest.Id, PlayerFactionModel.GetQueueLimit(unitRequest)) ||
                unitRequest is SecondStationUnitRequest && !_enemyStructurePlacementService.TryGetSecondStationPosition(out _) ||
                !_productionAccess.CanProduce ||
                unitRequest is LevelUnitRequest &&
                (_productionAccess.ProductionHost != Owner || !_productionAccess.HasHostBase))
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "purchaseRejected",
                    $"unit={unitRequest.Id};reason=queue or production unavailable");
                return;
            }

            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    if (IsUpgradingStation || !_wallet.TrySpend(levelUnitRequest))
                    {
                        return;
                    }

                    break;
                case SecondStationLevelUnitRequest:
                    if (IsUpgradingSecondStation || !_wallet.TrySpend(unitRequest)) return;
                    break;
                case ShipUnitRequest:
                    if (!TryReserveAndSpend(unitRequest)) return;
                    _unitLimitModel.RecordShipOrder();
                    break;
                // A research reservation blocks a second purchase of the line until the tier completes.
                case ResearchUnitRequest:
                case SecondStationUnitRequest:
                case SquadronUnitRequest:
                case MiningFacilityUnitRequest:
                case DefendPlatformUnitRequest:
                    if (!TryReserveAndSpend(unitRequest)) return;
                    break;
                default:
                    return;
            }

            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "purchaseAccepted",
                $"unit={unitRequest.Id};population={_unitLimitModel.CurrentUnitCapacity}");
            // Platforms and miners wait their queue build time like a human's, then are placed and built on site (W69).
            _builds.Add(unitRequest, unitRequest.FactionData.BuildTime);
        }

        public void PurchaseDefense(UnitRequest unitRequest, bool allowExtra, bool firstOnly)
        {
            _defenseFlags[unitRequest] = (allowExtra, firstOnly);
            Purchase(unitRequest);
            if (!_builds.Builds.Any(build => build.Request == unitRequest)) _defenseFlags.Remove(unitRequest);
        }

        public void Tick()
        {
            if (!_outerWingCountSent)
            {
                _squadronCommander.SetOuterWingCount(
                    EnemyWingPricing.HasCheapFighterWing(Model.SquadronFactionData, _profiles) ? 2 : 1);
                _outerWingCountSent = true;
            }
            _squadronCommander.SetWingPatrol(Model.CurrentLevel >= 2);
            bool hasRally = _aiState.TryGetReinforcementRally(out Vector3 rally, out bool defendHome);
            _squadronCommander.SetStationDefense(hasRally && defendHome ? rally : (Vector3?)null);
            _squadronCommander.SetZoneScoutTarget(_aiState.ZoneScoutTarget);
            _aiState.ReportReinforcementBudget(GetReinforcementBudget());
            if (!defendHome) DropHeldReinforcement();
            bool hasStation = _productionAccess.TryGetProductionBase(out var station);
            if (Time.time >= _stationTelemetryAt || hasStation != _hadStation)
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "station",
                    hasStation ? $"entity={station.Id};hull={station.HealthModel.Hull};shields={station.HealthModel.Shields};defendHome={defendHome};secondLevel={(_secondStation.IsBuilt ? _secondStation.CurrentLevel : 0)}" : "No living production station.");
                _stationTelemetryAt = Time.time + 15f;
                _hadStation = hasStation;
            }
            AdvanceBuilds(Time.deltaTime, defendHome);
        }

        public void Advance(float deltaTime)
        {
            _aiState.TryGetReinforcementRally(out _, out bool defendHome);
            AdvanceBuilds(deltaTime, defendHome);
        }

        /// <summary>
        /// W63 addendum: what this AI can still add to a fight: its bank, the credits its free unit capacity takes at the
        /// average price per slot of its unlocked ships, and the seconds until a ship bought now is on the map (a free
        /// production pipeline or the shortest running build, plus the average build time).
        /// </summary>
        private EnemyReinforcementBudget GetReinforcementBudget()
        {
            float price = 0f;
            float slots = 0f;
            float buildTime = 0f;
            int types = 0;
            foreach (KeyValuePair<ShipType, FactionData> ship in Model.ShipFactionData)
            {
                if (ship.Value.AvailableLevel > Model.CurrentLevel) continue;
                price += ship.Value.Price;
                slots += ship.Value.UnitCapacity;
                buildTime += ship.Value.BuildTime;
                types++;
            }
            int free = Math.Max(0, _reinforcementData.MaxUnitCapacity - _unitLimitModel.CurrentUnitCapacity);
            float capacityCredits = slots > 0f ? free * price / slots : 0f;
            float readySeconds = (types > 0 ? buildTime / types : 0f) + GetPipelineWaitSeconds();
            return new EnemyReinforcementBudget(_economyModel.Money, capacityCredits, readySeconds);
        }

        /// <summary>Zero while a production pipeline is free; otherwise the time left on the shortest running build.</summary>
        private float GetPipelineWaitSeconds()
        {
            _busyPipelines.Clear();
            float shortest = float.PositiveInfinity;
            foreach (EnemyPendingBuild build in _builds.Builds)
            {
                if (build.TimeLeft <= 0f) continue;
                _busyPipelines.Add(build.Request.Id);
                shortest = Mathf.Min(shortest, build.TimeLeft);
            }
            return _busyPipelines.Count >= PlayerFactionModel.MAX_ACTIVE_PIPELINES ? shortest : 0f;
        }

        /// <summary>
        /// D3: a group ship fights on its way and the group keeps going. A finished ship held off-map lands at the
        /// nearest valid spawn point beside it; without a held ship the request waits until it lapses.
        /// </summary>
        private void DropHeldReinforcement()
        {
            UnitRequest held = null;
            foreach (EnemyPendingBuild build in _builds.Builds)
            {
                if (!build.IsReadyForDeployment || build.Request is not ShipUnitRequest) continue;
                held = build.Request;
                break;
            }
            if (held == null || !_aiState.TryTakeReinforcementDrop(out Vector3 point)) return;
            _builds.RemoveAll(request => request == held);
            _dropPoint = point;
            try
            {
                ExecuteBuild(held);
            }
            finally
            {
                _dropPoint = null;
            }
        }

        /// <summary>True when an unlocked assault capital fits the free unit capacity.</summary>
        private bool AssaultCapitalFits()
        {
            int free = _reinforcementData.MaxUnitCapacity - _unitLimitModel.CurrentUnitCapacity;
            foreach (KeyValuePair<ShipType, FactionData> ship in Model.ShipFactionData)
                if (ship.Value.AvailableLevel <= Model.CurrentLevel && ship.Value.UnitCapacity <= free &&
                    Model.IsAssaultCapital(ship.Key, _profiles.Get(UnitTypeId.Ship(ship.Key)).ShipClass))
                    return true;
            return false;
        }

        private void AdvanceBuilds(float deltaTime, bool defendHome)
        {
            bool assault = !defendHome && _aiState.CurrentState == EnemyStrategicState.AssaultBase;
            if (Model.CurrentLevel >= 4)
            {
                bool hasHeldReserve = _builds.Builds.Any(build => build.IsReadyForDeployment &&
                    build.Request is ShipUnitRequest);
                bool hasCapitalBuilding = _builds.Builds.Any(build => build.TimeLeft > 0f &&
                    build.Request is ShipUnitRequest ship && Model.IsAssaultCapital(ship.Key,
                        _profiles.Get(UnitTypeId.Ship(ship.Key)).ShipClass));
                if (hasHeldReserve && hasCapitalBuilding) _assaultWavePlanUntil = Time.time + ASSAULT_WAVE_INTERVAL;
                // At the unit cap no assault capital fits, so there is nothing to wait for: the plan counts as met (W33).
                Model.HasAssaultReinforcementPlan = !AssaultCapitalFits() || hasCapitalBuilding &&
                    (hasHeldReserve || assault && Time.time < _assaultWavePlanUntil);
            }
            bool holdReserve = _productionAccess.CanProduce && (defendHome || _aiState.HoldFinishedReinforcements);
            // Opening: each ship deploys as soon as it is built, one by one, until the first outer zone is held.
            bool landingOpeningFleet = !defendHome &&
                (_aiState.ActiveShipCount < MINIMUM_REINFORCEMENT_WAVE || !_aiState.HoldsOuterZone);
            int minimumGroup = !holdReserve || landingOpeningFleet ? 0 :
                defendHome ? MINIMUM_DEFENSE_WAVE : MINIMUM_REINFORCEMENT_WAVE;
            bool allowLoneUnit = assault || !defendHome &&
                                 _unitLimitModel.CurrentUnitCapacity >= _reinforcementData.MaxUnitCapacity;
            _landedShips.Clear();
            _builds.Advance(deltaTime, ExecuteBuild, minimumGroup,
                allowLoneUnit ? LONE_REINFORCEMENT_WAIT_SECONDS : DEPLOYMENT_EMERGENCY_SECONDS,
                batchDeploymentRetries: defendHome,
                deployWave: defendHome && holdReserve ? DeployDefenseWave : null,
                maxBlockedHoldSeconds: MAX_DEPLOYMENT_HOLD_SECONDS, shipDeployOrder: _smallestShipFirst);
            Model.HeldShips = _builds.CountHeldShips();
        }

        // Small hulls land first: they fit more points, and each one widens the fog vision for the next.
        private int CompareHullSize(UnitRequest left, UnitRequest right) =>
            _shipSpawnPoints.GetHullRadius(((ShipUnitRequest)left).Key)
                .CompareTo(_shipSpawnPoints.GetHullRadius(((ShipUnitRequest)right).Key));

        private void DeployDefenseWave(IReadOnlyList<UnitRequest> wave)
        {
            if (!_productionAccess.CanProduce || !_aiState.TryGetReinforcementRally(out Vector3 rally, out bool defense) ||
                !defense)
            {
                foreach (UnitRequest request in wave) ExecuteBuild(request);
                return;
            }

            PlayerId home = GetDeploymentHost();
            _aiState.FillDefenseDeploymentThreats(_defenseDeploymentThreats);
            int squadronCount = 0;
            bool anyPastHoldLimit = false;
            try
            {
                foreach (UnitRequest request in wave)
                {
                    if (request is not ShipUnitRequest ship)
                    {
                        squadronCount++;
                        continue;
                    }

                    float heldSeconds = _builds.GetDeploymentBlockedSeconds(ship);
                    anyPastHoldLimit |= heldSeconds >= MAX_DEPLOYMENT_HOLD_SECONDS;
                    bool found = TryFindShipSpawnPosition(home, ship.Key, hasRally: true, defendHome: true, rally,
                        heldSeconds, out Vector3 position, out string tier,
                        out bool forced, out bool emergency);
                    if (!found)
                    {
                        EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deploymentBlocked",
                            FormattableString.Invariant(
                                $"unit={ship.Id};homeDefense=True;rally={rally};tier={tier};heldSeconds={heldSeconds:F1}"));
                        continue;
                    }

                    _shipSpawnPoints.ReserveDefenseLanding(ship, home, ship.Key, position);
                    _defenseLandings.Add(ship, (position, tier, forced, emergency));
                }

                bool canDeploy = _defenseLandings.Count + squadronCount >= MINIMUM_DEFENSE_WAVE ||
                    anyPastHoldLimit && (_defenseLandings.Count > 0 || squadronCount > 0);
                foreach (UnitRequest request in wave)
                {
                    if (request is ShipUnitRequest ship && (!canDeploy || !_defenseLandings.ContainsKey(ship)))
                        _builds.RetryShipDeployment(ship, DEPLOYMENT_RETRY_INTERVAL);
                    else if (request is SquadronUnitRequest squadron && !canDeploy)
                        _builds.RetrySquadronDeployment(squadron, DEPLOYMENT_RETRY_INTERVAL);
                    else
                        ExecuteBuild(request);
                }
            }
            finally
            {
                foreach (ShipUnitRequest ship in _defenseLandings.Keys)
                    _shipSpawnPoints.ReleaseDefenseLanding(ship);
                _defenseLandings.Clear();
            }
        }

        private bool TryFindShipSpawnPosition(
            PlayerId home,
            ShipType shipType,
            bool hasRally,
            bool defendHome,
            Vector3 rally,
            float heldSeconds,
            out Vector3 position,
            out string tier,
            out bool forced,
            out bool emergency)
        {
            // Rear mode only for a defended station or one under pressure; otherwise the forward search ran first.
            bool useRearAnchor = hasRally && defendHome;
            Vector3 anchor = rally;
            forced = false;
            emergency = false;

            if (heldSeconds >= DEPLOYMENT_EMERGENCY_SECONDS && _productionAccess.CanProduce)
            {
                tier = "D";
                forced = true;
                emergency = true;
                if (TryTierDSpawn(home, shipType, out position)) return true;
                // No clear point anywhere on the map: retry next second rather than spawn into a hull or the enemy.
                tier = "E";
                forced = false;
                emergency = false;
                return false;
            }

            // Tier A: full safety filter (rear ring search with IsSafe + CanSpawnShip, or normal rally/default).
            tier = "A";
            bool found = useRearAnchor
                ? _shipSpawnPoints.TryGetRearSpawnPosition(home, shipType, anchor, _defenseDeploymentThreats,
                    requireThreatSafety: true, out position)
                : hasRally
                    ? _shipSpawnPoints.TryGetSpawnPositionNear(home, shipType, rally, out position)
                    : TryTierDSpawn(home, shipType, out position);
            if (found) return true;

            // Tier B (after 4 s): ignore remembered threats, keep CanSpawnShip clearance on rear rings.
            if (heldSeconds >= DEPLOYMENT_TIER_B_SECONDS && useRearAnchor)
            {
                tier = "B";
                if (_shipSpawnPoints.TryGetRearSpawnPosition(home, shipType, anchor, _defenseDeploymentThreats,
                        requireThreatSafety: false, out position))
                    return true;
            }

            // Tier C (after 8 s): station-defense rule on the widest ring (or rally/default fallback).
            if (heldSeconds >= MAX_DEPLOYMENT_HOLD_SECONDS && _productionAccess.CanProduce)
            {
                tier = "C";
                forced = true;
                found = useRearAnchor
                    ? _shipSpawnPoints.TryGetWideDefenseSpawnPosition(home, shipType, anchor,
                        _defenseDeploymentThreats, out position)
                    : hasRally && _shipSpawnPoints.TryGetSpawnPositionNear(home, shipType, rally, out position) ||
                      TryTierDSpawn(home, shipType, out position);
                if (found) return true;
            }

            // Tier D (after 12 s): station -> home zone -> any held zone -> random fallback.
            if (heldSeconds >= DEPLOYMENT_TIER_D_SECONDS && _productionAccess.CanProduce)
            {
                tier = "D";
                forced = true;
                if (TryTierDSpawn(home, shipType, out position)) return true;
            }

            position = default;
            forced = false;
            return false;
        }

        /// <summary>
        /// Forward mode: beside the spawn beacon, wings, ships or structures nearest the ship's task. A planned attack
        /// wave (W68) uses <see cref="IEnemyForwardSpawnAnchors.CollectPlannedWave"/>: the human spawn rule, no escort filter.
        /// </summary>
        private bool TryForwardSpawn(PlayerId home, ShipUnitRequest ship, Vector3 task, bool plannedWave,
            out Vector3 position, out string anchor)
        {
            position = default;
            anchor = "none";
            Vector3 awayFrom;
            bool collected = plannedWave
                ? _forwardAnchors.CollectPlannedWave(task, _landedShips, _anchors, out awayFrom)
                : _forwardAnchors.Collect(task, _landedShips, IsAssaultCapital(ship), _anchors, out awayFrom);
            if (!collected)
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "spawnAttempt",
                    $"unit={ship.Id};units=0;points=0;notOpen=0;notClear=0;found=False");
                return false;
            }
            _anchorPositions.Clear();
            foreach (ForwardSpawnAnchor candidate in _anchors) _anchorPositions.Add(candidate.Position);
            bool found = _shipSpawnPoints.TryGetSpawnPositionNearUnits(home, ship.Key, _anchorPositions, awayFrom,
                out position, out ForwardSpawnAttempt attempt);
            if (found) anchor = _anchors[attempt.UnitIndex].Kind;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "spawnAttempt",
                $"unit={ship.Id};units={_anchors.Count};points={attempt.Points};notOpen={attempt.NotOpen};notClear={attempt.NotClear};found={found};anchor={anchor}");
            return found;
        }

        // Assault capitals unlock at level 4; the class is read only for those.
        private bool IsAssaultCapital(ShipUnitRequest ship) => ship.FactionData.AvailableLevel >= 4 &&
            Model.IsAssaultCapital(ship.Key, _profiles.Get(UnitTypeId.Ship(ship.Key)).ShipClass);

        private bool TryTierDSpawn(PlayerId home, ShipType shipType, out Vector3 position) =>
            _shipSpawnPoints.TryGetStationSpawnPosition(home, shipType, out position) ||
            _shipSpawnPoints.TryGetDefaultZoneSpawnPosition(home, shipType, out position) ||
            _shipSpawnPoints.TryGetRandomSpawnPosition(home, shipType, out position);

        private PlayerId GetDeploymentHost() =>
            !_productionAccess.HasHostBase && _productionAccess.TryGetProductionBase(out var productionBase)
                ? productionBase.Owner : _productionAccess.ProductionHost;

        /// <summary>Puts back a saved build: its reservation is taken again and it keeps its time left.</summary>
        public void RestorePendingBuild(UnitRequest unitRequest, float timeLeft)
        {
            AdoptReservation(unitRequest);
            // A superseded second-station upgrade is refunded when it completes.
            _builds.Add(unitRequest, timeLeft);
        }

        /// <summary>Counts a restored unit against the limits again and frees its slot when it is gone.</summary>
        public void AdoptShip(ShipEntity ship, ShipUnitRequest unitRequest)
        {
            AdoptReservation(unitRequest);
            Track(ship, unitRequest);
        }

        public void AdoptSquadron(ISquadron squadron, SquadronUnitRequest unitRequest)
        {
            AdoptReservation(unitRequest);
            Track(squadron, unitRequest);
        }

        public void AdoptMiningFacility(MiningFacilityEntity facility, MiningFacilityUnitRequest unitRequest)
        {
            AdoptReservation(unitRequest);
            Track(facility, unitRequest);
        }

        public void AdoptDefendPlatform(DefendPlatformEntity platform, DefendPlatformUnitRequest unitRequest)
        {
            AdoptReservation(unitRequest);
            Track(platform, unitRequest);
        }

        private void AdoptReservation(UnitRequest unitRequest) => _unitLimitModel.Adopt(unitRequest);

        private void Build(UnitRequest unitRequest)
        {
            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    Model.CompleteLevelUpgrade();
                    // Frees the slot a restored upgrade adopted; a purchased upgrade holds none.
                    ReleaseUnit(levelUnitRequest);
                    break;
                case ResearchUnitRequest researchUnitRequest:
                    _research.Complete(researchUnitRequest.Key);
                    ReleaseUnit(researchUnitRequest);
                    break;
                case SecondStationLevelUnitRequest secondLevelRequest:
                    _secondStation.CompleteLevelUpgrade();
                    // Frees the slot a restored upgrade adopted; a purchased upgrade holds none.
                    ReleaseUnit(secondLevelRequest);
                    break;
                case ShipUnitRequest shipUnitRequest:
                    bool hasRally = _aiState.TryGetReinforcementRally(out Vector3 rally, out bool defendHome);
                    if (defendHome && !_productionAccess.CanProduce)
                    {
                        // The strategic snapshot can still name a defense rally after its station falls.
                        hasRally = false;
                        defendHome = false;
                    }
                    bool plannedWave = false;
                    if (_dropPoint is Vector3 drop)
                    {
                        // D3: beside the group ship that fights on its way.
                        rally = drop;
                        hasRally = true;
                        defendHome = false;
                    }
                    else if (!defendHome && _aiState.TryGetPlannedWave(out Vector3 wave))
                    {
                        // W68: the planned attack wave drops beside the scout or nearest own unit at the target.
                        rally = wave;
                        hasRally = true;
                        plannedWave = true;
                    }
                    // Ships arrive at the home of whichever team base builds them, borrowed from an ally or not.
                    PlayerId home = GetDeploymentHost();
                    float heldSeconds = _builds.GetDeploymentBlockedSeconds(shipUnitRequest);
                    Vector3 position;
                    bool foundPosition;
                    string tier;
                    bool forced;
                    bool emergency;
                    string anchor;
                    bool anchorContested = false;
                    // W56: with no living own station an assault capital lands beside the group body or not at all.
                    bool bodyOnlyCapital = !defendHome && !HasOwnLivingStation && IsAssaultCapital(shipUnitRequest);
                    if (_defenseLandings.Remove(shipUnitRequest, out var landing))
                    {
                        _shipSpawnPoints.ReleaseDefenseLanding(shipUnitRequest);
                        position = landing.Position;
                        tier = landing.Tier;
                        forced = landing.Forced;
                        emergency = landing.Emergency;
                        foundPosition = true;
                        anchor = "rear";
                    }
                    else if ((hasRally || bodyOnlyCapital) && !defendHome &&
                             TryForwardSpawn(home, shipUnitRequest, rally, plannedWave, out position, out anchor))
                    {
                        tier = "F";
                        forced = false;
                        emergency = false;
                        foundPosition = true;
                    }
                    else if (bodyOnlyCapital)
                    {
                        // No escorted body to join: held, then refunded instead of landing alone at a relay or ally base.
                        tier = "body";
                        anchor = "none";
                        forced = false;
                        emergency = false;
                        foundPosition = false;
                        position = default;
                        if (heldSeconds >= DEPLOYMENT_EMERGENCY_SECONDS || !_productionAccess.CanProduce)
                        {
                            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "capitalRefunded",
                                FormattableString.Invariant(
                                    $"unit={shipUnitRequest.Id};reason=noStationNoBody;heldSeconds={heldSeconds:F1}"));
                            _builds.ClearDeploymentBlock(shipUnitRequest);
                            _unitLimitModel.CancelShipOrder();
                            ReleaseUnit(shipUnitRequest);
                            _wallet.Refund(shipUnitRequest);
                            return;
                        }
                    }
                    else
                    {
                        if (hasRally && defendHome)
                            _aiState.FillDefenseDeploymentThreats(_defenseDeploymentThreats);
                        // No forward point: the held relay or station search runs in the same tick.
                        // An assault capital lands at a relay only when it is held, uncontested and clear of hostile
                        // ships; else at home (the forward search beside the body already ran) (W38).
                        anchorContested = hasRally && !defendHome && !_aiState.IsCapitalRallySafe(rally);
                        bool capitalHome = anchorContested && IsAssaultCapital(shipUnitRequest);
                        foundPosition = TryFindShipSpawnPosition(home, shipUnitRequest.Key, hasRally && !capitalHome,
                            defendHome, rally, heldSeconds, out position, out tier, out forced, out emergency);
                        anchor = defendHome ? "rear" : capitalHome ? "homeCapital" : "relayOrStation";
                    }
                    if (!foundPosition)
                    {
                        if (_productionAccess.CanProduce)
                        {
                            // Keep the completed ship paid and reserved while its arrival area is blocked.
                            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deploymentBlocked",
                                FormattableString.Invariant(
                                    $"unit={shipUnitRequest.Id};homeDefense={defendHome};rally={rally};tier={tier};heldSeconds={heldSeconds:F1}"));
                            _builds.RetryShipDeployment(shipUnitRequest, DEPLOYMENT_RETRY_INTERVAL);
                            return;
                        }

                        // An impossible arrival must not keep a defeated faction alive indefinitely.
                        throw new InvalidOperationException(
                            $"No open enemy spawn position is available near a held relay or home for {shipUnitRequest.Key}.");
                    }

                    if (emergency)
                        EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deploymentEmergency",
                            FormattableString.Invariant(
                                $"unit={shipUnitRequest.Id};rally={rally};spawn={position};tier={tier};heldSeconds={heldSeconds:F1}"));
                    else if (forced)
                        EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deploymentForced",
                            FormattableString.Invariant(
                                $"unit={shipUnitRequest.Id};rally={rally};spawn={position};tier={tier};heldSeconds={heldSeconds:F1}"));
                    EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deployment",
                        FormattableString.Invariant(
                            $"unit={shipUnitRequest.Id};homeDefense={defendHome};rally={rally};spawn={position};tier={tier};anchor={anchor};anchorContested={anchorContested};heldSeconds={heldSeconds:F1};plannedWave={plannedWave};drop={_dropPoint.HasValue}"));
                    _builds.ClearDeploymentBlock(shipUnitRequest);
                    Track(_shipFactory.Create(Owner, shipUnitRequest.Key, position), shipUnitRequest);
                    _landedShips.Add(position);
                    break;
                case SquadronUnitRequest squadronUnitRequest:
                    if (!_squadronLauncher.TryLaunchFromStation(Owner, squadronUnitRequest.Key, out ISquadron squadron))
                    {
                        // Every team base fell during the build, so nothing can launch the squadron.
                        ReleaseUnit(squadronUnitRequest);
                        _wallet.Refund(squadronUnitRequest);
                        Debug.Log(
                            $"[EnemyAI:Production] {squadronUnitRequest.Id} not launched: " +
                            "no living station. Purchase refunded.");
                        return;
                    }

                    EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deployment",
                        $"unit={squadronUnitRequest.Id};source=station");
                    Track(squadron, squadronUnitRequest);
                    break;
                case SecondStationUnitRequest stationRequest:
                    if (!_enemyStructurePlacementService.TryGetSecondStationPosition(out Vector3 stationPosition))
                    {
                        // The paid station waits for a point instead of a refund-and-rebuy loop.
                        _enemyStructurePlacementService.RecordDeferred(stationRequest.Id, 0f);
                        _builds.RetryStructurePlacement(stationRequest, STATION_PLACEMENT_RETRY_SECONDS);
                        return;
                    }
                    Track(_stationFactory.CreateSecond(Owner, _owner.Faction, stationPosition), stationRequest);
                    Model.AdditionalStationsBuilt++;
                    _enemyStructurePlacementService.RecordPlacement(stationRequest.Id, stationPosition);
                    break;
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                    if (!TryGetStructurePosition(unitRequest, out Vector3 minerPosition)) return;
                    Track(_miningFacilityFactory.Create(Owner, miningFacilityUnitRequest.Key, minerPosition),
                        miningFacilityUnitRequest);
                    break;
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                    if (!TryGetStructurePosition(unitRequest, out Vector3 platformPosition)) return;
                    _defenseFlags.Remove(unitRequest);
                    Track(_defendPlatformFactory.Create(Owner, defendPlatformUnitRequest.Key, platformPosition),
                        defendPlatformUnitRequest);
                    break;
            }
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "buildCompleted", $"unit={unitRequest.Id}");
        }

        private void Track(ShipEntity ship, ShipUnitRequest unitRequest) =>
            ship.OnRelease += _ => ReleaseUnit(unitRequest);

        private void Track(ISquadron squadron, SquadronUnitRequest unitRequest)
        {
            long squadronId = squadron.EntityId;
            Action handler = null;
            handler = () =>
            {
                squadron.Released -= handler;
                _builtSquadrons.Remove(squadronId);
                ReleaseUnit(unitRequest);
            };
            squadron.Released += handler;
            _builtSquadrons.Add(squadronId);
            _squadronCommander.Command(squadron, unitRequest.Key);
        }

        private void Track(MiningFacilityEntity facility, MiningFacilityUnitRequest unitRequest) =>
            facility.OnRelease += () =>
            {
                _enemyStructurePlacementService.RecordDestroyedPosition(facility.transform.position);
                ReleaseUnit(unitRequest);
            };

        public void AdoptSecondStation(SpaceStationEntity station, SecondStationUnitRequest unitRequest)
        {
            // A living second station in an older save establishes at least one completed build.
            if (Model.AdditionalStationsBuilt == 0)
                Model.AdditionalStationsBuilt = 1;
            AdoptReservation(unitRequest);
            Track(station, unitRequest);
        }

        private void Track(SpaceStationEntity station, SecondStationUnitRequest unitRequest) =>
            station.Destroyed += () =>
            {
                _enemyStructurePlacementService.RecordDestroyedPosition(station.transform.position);
                ReleaseUnit(unitRequest);
            };

        private void Track(DefendPlatformEntity platform, DefendPlatformUnitRequest unitRequest)
        {
            _platforms.Add(platform);
            platform.OnRelease += () =>
            {
                _platforms.Remove(platform);
                _enemyStructurePlacementService.RecordDestroyedPosition(platform.transform.position);
                ReleaseUnit(unitRequest);
            };
        }

        private bool TryReserveAndSpend(UnitRequest unitRequest)
        {
            FactionData factionData = unitRequest.FactionData;
            // The human purchase limits (D6): unique units (heroes, dreadnoughts) once, structures to MaxCount, other ships and squadrons by population only.
            if (!_unitLimitModel.TryReserve(unitRequest, _reinforcementData.MaxUnitCapacity))
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "purchaseRejected",
                    $"unit={unitRequest.Id};reason=capacity or unit limit;population={_unitLimitModel.CurrentUnitCapacity};required={factionData.UnitCapacity}");
                return false;
            }

            if (_wallet.TrySpend(unitRequest))
            {
                return true;
            }

            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "purchaseRejected",
                $"unit={unitRequest.Id};reason=insufficient credits");
            ReleaseUnit(unitRequest);
            return false;
        }

        private void RecalculateIncome(int level) => _economyProvider.RecalculateIncome(this);

        private void HandleSecondStationChanged(bool built)
        {
            _economyProvider.RecalculateIncome(this);
            if (built) return;
            // A lost second station takes its queued upgrade with it.
            foreach (EnemyPendingBuild upgrade in _builds.Builds.Where(build => build.Request is SecondStationLevelUnitRequest))
            {
                ReleaseUnit(upgrade.Request);
                _wallet.Refund(upgrade.Request);
            }
            _builds.RemoveAll(request => request is SecondStationLevelUnitRequest);
        }

        /// <summary>The second station stands, the upgrade is its next level, and it stays at or below the main level.</summary>
        private bool CanUpgradeSecondStation(SecondStationLevelUnitRequest request) =>
            _secondStation.IsBuilt && request.Level == _secondStation.CurrentLevel &&
            _secondStation.CurrentLevel < Model.CurrentLevel;

        private void ReleaseUnit(UnitRequest unitRequest) => _unitLimitModel.Release(unitRequest);

        private void ExecuteBuild(UnitRequest unitRequest)
        {
            if (unitRequest is SecondStationLevelUnitRequest secondLevel &&
                (!_secondStation.IsBuilt || secondLevel.Level != _secondStation.CurrentLevel) ||
                unitRequest is SecondStationUnitRequest && Model.AdditionalStationsBuilt > 0 ||
                (unitRequest is LevelUnitRequest
                    ? !_productionAccess.HasHostBase
                    : !_productionAccess.CanProduce && _productionAccess.ProductionHost != Owner &&
                      unitRequest is not SecondStationUnitRequest))
            {
                if (unitRequest is ShipUnitRequest) _unitLimitModel.CancelShipOrder();
                _defenseFlags.Remove(unitRequest);
                ReleaseUnit(unitRequest);
                _wallet.Refund(unitRequest);
                return;
            }
            // Queued reinforcements still arrive after the station falls so the fleet victory can resolve.
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (BattleProfilerMarkers.EnemyBuild.Auto())
#endif
                {
                    Build(unitRequest);
                }
            }
            catch (Exception exception)
            {
                if (unitRequest is ShipUnitRequest)
                {
                    _unitLimitModel.CancelShipOrder();
                }
                _defenseFlags.Remove(unitRequest);
                ReleaseUnit(unitRequest);
                _wallet.Refund(unitRequest);
                Debug.LogError(
                    $"[EnemyAI:Production] Build failed for " +
                    $"{unitRequest.GetType().Name} ({unitRequest.Id}). " +
                    $"Purchase refunded.\n{exception}");
            }
        }

        private void CancelPendingBuilds()
        {
            foreach (EnemyPendingBuild pendingBuild in _builds.Builds)
            {
                if (pendingBuild.Request is ShipUnitRequest)
                {
                    _unitLimitModel.CancelShipOrder();
                }
                ReleaseUnit(pendingBuild.Request);
            }

            _builds.Clear();
            _defenseFlags.Clear();
        }

        /// <summary>
        /// A platform uses the flags it was bought with (a restored build: extra platforms allowed, as before W50);
        /// a miner the structure search. No point → the paid build stays queued and retries (W69).
        /// </summary>
        private bool TryGetStructurePosition(UnitRequest request, out Vector3 position)
        {
            bool found;
            if (request is DefendPlatformUnitRequest)
            {
                (bool allowExtra, bool firstOnly) = _defenseFlags.TryGetValue(request, out var flags) ? flags : (true, false);
                found = _enemyStructurePlacementService.TryGetDefensePosition(allowExtra, firstOnly,
                    EnemyPlatformTier.IsBig(request.FactionData), out position);
            }
            else
                found = _enemyStructurePlacementService.TryGetPosition(out position);
            if (found)
            {
                _enemyStructurePlacementService.RecordPlacement(request.Id, position);
                return true;
            }

            if (!_productionAccess.CanProduce)
            {
                // No team base: nothing places it, and a held build would keep a defeated faction alive.
                _enemyStructurePlacementService.RecordDeferred(request.Id, request.FactionData.Price);
                _defenseFlags.Remove(request);
                ReleaseUnit(request);
                _wallet.Refund(request);
                return false;
            }

            _enemyStructurePlacementService.RecordDeferred(request.Id, 0f);
            _builds.RetryStructurePlacement(request, STRUCTURE_PLACEMENT_RETRY_SECONDS);
            return false;
        }
    }
}
