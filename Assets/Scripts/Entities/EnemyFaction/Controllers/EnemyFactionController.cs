using System;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
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
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
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
        private const float DEFAULT_INCOME = 15f;
        private const float DEPLOYMENT_RETRY_INTERVAL = 1f;
        private const float MAX_DEPLOYMENT_HOLD_SECONDS = 8f;
        private const float LONE_REINFORCEMENT_WAIT_SECONDS = 12f;
        private const int MINIMUM_DEFENSE_WAVE = 2;
        private const int MINIMUM_REINFORCEMENT_WAVE = 3;
        private const float ASSAULT_WAVE_INTERVAL = 30f;

        private readonly IEconomyProvider _economyProvider;
        private readonly IWallet _wallet;
        private readonly SpaceStationFactory _stationFactory;
        private readonly UnitCombatProfileCatalog _profiles;
        private readonly IShipSpawnPoints _shipSpawnPoints;
        private readonly IEnemyAiStateProvider _aiState;
        private float _stationTelemetryAt;
        private float _assaultWavePlanUntil;
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
        private readonly Dictionary<ShipUnitRequest, (Vector3 Position, bool Forced)> _defenseLandings =
            new Dictionary<ShipUnitRequest, (Vector3, bool)>();
        private readonly HashSet<long> _builtSquadrons = new HashSet<long>();
        private readonly MiningFacilityFactory _miningFacilityFactory;
        private readonly DefendPlatformFactory _defendPlatformFactory;
        private readonly FactionResearchModel _research;

        private bool _isInitialized;

        private PlayerId Owner => _owner.Id;
        public float Income => DEFAULT_INCOME * Model.CurrentLevel * _research.StationIncomeMultiplier;
        public bool HasPendingReinforcement => _builds.Count > 0;
        public bool PriorityConstructionPending { get; set; }
        public bool IsUpgradingStation => _builds.Builds.Any(build => build.Request is LevelUnitRequest);
        public bool HasActiveProduction => _builds.Builds.Any(build => build.TimeLeft > 0f);
        public IReadOnlyList<EnemyPendingBuild> PendingBuilds => _builds.Builds;

        public bool CanQueue(string requestId, int maxCount) =>
            (requestId != SecondStationUnitRequest.REQUEST_ID ||
             Model.AdditionalStationsBuilt == 0 && Model.CurrentLevel >= 2 &&
             _productionAccess.ProductionHost == Owner && _productionAccess.HasHostBase) &&
            _builds.CanAdd(requestId, maxCount);
        public float GetQueueRemainingTime(string requestId) => _builds.GetRemainingTime(requestId);

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
            UnitCombatProfileCatalog profiles) : base(model)
        {
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
            if (unitRequest is SecondStationLevelUnitRequest ||
                !CanQueue(unitRequest.Id, unitRequest.FactionData.MaxCount) ||
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
            if (unitRequest is MiningFacilityUnitRequest or DefendPlatformUnitRequest)
                ExecuteBuild(unitRequest);
            else
                _builds.Add(unitRequest, unitRequest.FactionData.BuildTime);
        }

        public void Tick()
        {
            bool hasRally = _aiState.TryGetReinforcementRally(out Vector3 rally, out bool defendHome);
            _squadronCommander.SetStationDefense(hasRally && defendHome ? rally : (Vector3?)null);
            bool hasStation = _productionAccess.TryGetProductionBase(out var station);
            if (Time.time >= _stationTelemetryAt || hasStation != _hadStation)
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "station",
                    hasStation ? $"entity={station.Id};hull={station.HealthModel.Hull};shields={station.HealthModel.Shields};defendHome={defendHome}" : "No living production station.");
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
                Model.HasAssaultReinforcementPlan = hasCapitalBuilding &&
                    (hasHeldReserve || assault && Time.time < _assaultWavePlanUntil);
            }
            bool holdReserve = _productionAccess.CanProduce && (defendHome || _aiState.HoldFinishedReinforcements);
            bool landingOpeningFleet = !defendHome && _aiState.ActiveShipCount < MINIMUM_REINFORCEMENT_WAVE;
            int minimumGroup = !holdReserve || landingOpeningFleet ? 0 :
                defendHome ? MINIMUM_DEFENSE_WAVE : MINIMUM_REINFORCEMENT_WAVE;
            bool allowLoneUnit = assault || !defendHome &&
                                 _unitLimitModel.CurrentUnitCapacity >= _reinforcementData.MaxUnitCapacity;
            _builds.Advance(deltaTime, ExecuteBuild, minimumGroup,
                allowLoneUnit ? LONE_REINFORCEMENT_WAIT_SECONDS : float.PositiveInfinity,
                batchDeploymentRetries: defendHome,
                deployWave: defendHome && holdReserve ? DeployDefenseWave : null);
        }

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
            try
            {
                foreach (UnitRequest request in wave)
                {
                    if (request is not ShipUnitRequest ship)
                    {
                        squadronCount++;
                        continue;
                    }

                    bool found = _shipSpawnPoints.TryGetDefenseSpawnPosition(home, ship.Key, rally,
                        _defenseDeploymentThreats, out Vector3 position);
                    bool forced = !found && _builds.GetDeploymentBlockedSeconds(ship) >= MAX_DEPLOYMENT_HOLD_SECONDS;
                    if (forced)
                        found = _shipSpawnPoints.TryGetBestDefenseSpawnPosition(home, ship.Key, rally,
                            _defenseDeploymentThreats, out position);
                    if (!found)
                    {
                        EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deploymentBlocked",
                            $"unit={ship.Id};homeDefense=True;rally={rally}");
                        continue;
                    }

                    _shipSpawnPoints.ReserveDefenseLanding(ship, home, ship.Key, position);
                    _defenseLandings.Add(ship, (position, forced));
                }

                bool canDeploy = _defenseLandings.Count + squadronCount >= MINIMUM_DEFENSE_WAVE;
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

        private PlayerId GetDeploymentHost() =>
            !_productionAccess.HasHostBase && _productionAccess.TryGetProductionBase(out var productionBase)
                ? productionBase.Owner : _productionAccess.ProductionHost;

        /// <summary>Puts back a saved build: its reservation is taken again and it keeps its time left.</summary>
        public void RestorePendingBuild(UnitRequest unitRequest, float timeLeft)
        {
            AdoptReservation(unitRequest);
            // Superseded second-station upgrades are refunded after the economy has loaded.
            _builds.Add(unitRequest, unitRequest is SecondStationLevelUnitRequest ? 0f : timeLeft);
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

        private void AdoptReservation(UnitRequest unitRequest) =>
            _unitLimitModel.Adopt(UnitLimitKey.From(unitRequest), unitRequest.FactionData.UnitCapacity);

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
                case ShipUnitRequest shipUnitRequest:
                    bool hasRally = _aiState.TryGetReinforcementRally(out Vector3 rally, out bool defendHome);
                    if (defendHome && !_productionAccess.CanProduce)
                    {
                        // The strategic snapshot can still name a defense rally after its station falls.
                        hasRally = false;
                        defendHome = false;
                    }
                    // Ships arrive at the home of whichever team base builds them, borrowed from an ally or not.
                    PlayerId home = GetDeploymentHost();
                    Vector3 position;
                    bool foundPosition;
                    Vector3 stationAnchor = default;
                    bool hasRear = !defendHome && _productionAccess.TryGetProductionBase(out var baseEntity) &&
                        _aiState.TryGetRearDeploymentThreats(stationAnchor =
                            baseEntity.GetFacade<IEntityTransformFacade>().Transform.position, _defenseDeploymentThreats);
                    if (_defenseLandings.Remove(shipUnitRequest, out var landing))
                    {
                        _shipSpawnPoints.ReleaseDefenseLanding(shipUnitRequest);
                        position = landing.Position;
                        foundPosition = true;
                        if (landing.Forced)
                            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deploymentForced",
                                $"unit={shipUnitRequest.Id};rally={rally};spawn={position}");
                    }
                    else if (hasRally && defendHome)
                    {
                        _aiState.FillDefenseDeploymentThreats(_defenseDeploymentThreats);
                        foundPosition = _shipSpawnPoints.TryGetDefenseSpawnPosition(home, shipUnitRequest.Key,
                            rally, _defenseDeploymentThreats, out position);
                    }
                    else if (hasRear)
                    {
                        foundPosition = _shipSpawnPoints.TryGetDefenseSpawnPosition(home, shipUnitRequest.Key,
                            stationAnchor, _defenseDeploymentThreats, out position);
                    }
                    else
                    {
                        foundPosition = hasRally
                            ? _shipSpawnPoints.TryGetSpawnPositionNear(home, shipUnitRequest.Key, rally, out position)
                            : _shipSpawnPoints.TryGetStationSpawnPosition(home, shipUnitRequest.Key, out position) ||
                              _shipSpawnPoints.TryGetDefaultZoneSpawnPosition(home, shipUnitRequest.Key, out position) ||
                              _shipSpawnPoints.TryGetRandomSpawnPosition(home, shipUnitRequest.Key, out position);
                    }
                    bool forced = !foundPosition && _productionAccess.CanProduce &&
                                  _builds.GetDeploymentBlockedSeconds(shipUnitRequest) >= MAX_DEPLOYMENT_HOLD_SECONDS;
                    if (forced)
                    {
                        foundPosition = hasRally && defendHome || hasRear
                            ? _shipSpawnPoints.TryGetBestDefenseSpawnPosition(home, shipUnitRequest.Key,
                                hasRear ? stationAnchor : rally,
                                _defenseDeploymentThreats, out position)
                            : hasRally && _shipSpawnPoints.TryGetSpawnPositionNear(home, shipUnitRequest.Key, rally, out position) ||
                              _shipSpawnPoints.TryGetStationSpawnPosition(home, shipUnitRequest.Key, out position) ||
                              _shipSpawnPoints.TryGetRandomSpawnPosition(home, shipUnitRequest.Key, out position);
                        if (foundPosition)
                            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deploymentForced",
                                $"unit={shipUnitRequest.Id};rally={rally};spawn={position}");
                    }
                    if (!foundPosition)
                    {
                        if (_productionAccess.CanProduce)
                        {
                            // Keep the completed ship paid and reserved while its arrival area is blocked.
                            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deploymentBlocked",
                                $"unit={shipUnitRequest.Id};homeDefense={defendHome};rally={rally}");
                            _builds.RetryShipDeployment(shipUnitRequest, DEPLOYMENT_RETRY_INTERVAL);
                            return;
                        }

                        // An impossible arrival must not keep a defeated faction alive indefinitely.
                        throw new InvalidOperationException(
                            $"No open enemy spawn position is available near a held relay or home for {shipUnitRequest.Key}.");
                    }

                    EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "deployment",
                        $"unit={shipUnitRequest.Id};homeDefense={defendHome};rally={rally};spawn={position}");
                    _builds.ClearDeploymentBlock(shipUnitRequest);
                    Track(_shipFactory.Create(Owner, shipUnitRequest.Key, position), shipUnitRequest);
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
                        _enemyStructurePlacementService.RecordDeferred(stationRequest.Id, stationRequest.FactionData.Price);
                        ReleaseUnit(stationRequest);
                        _wallet.Refund(stationRequest);
                        return;
                    }
                    Track(_stationFactory.CreateSecond(Owner, _owner.Faction, stationPosition), stationRequest);
                    Model.AdditionalStationsBuilt++;
                    _enemyStructurePlacementService.RecordPlacement(stationRequest.Id, stationPosition);
                    break;
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                    Track(_miningFacilityFactory.Create(Owner, miningFacilityUnitRequest.Key, GenerateMapCoordinates(unitRequest)),
                        miningFacilityUnitRequest);
                    break;
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                    Track(_defendPlatformFactory.Create(Owner, defendPlatformUnitRequest.Key, GenerateMapCoordinates(unitRequest)),
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

        private void Track(DefendPlatformEntity platform, DefendPlatformUnitRequest unitRequest) =>
            platform.OnRelease += () =>
            {
                _enemyStructurePlacementService.RecordDestroyedPosition(platform.transform.position);
                ReleaseUnit(unitRequest);
            };

        private bool TryReserveAndSpend(UnitRequest unitRequest)
        {
            FactionData factionData = unitRequest.FactionData;
            if (!_unitLimitModel.TryReserve(
                    UnitLimitKey.From(unitRequest),
                    factionData.MaxCount,
                    factionData.UnitCapacity,
                    _reinforcementData.MaxUnitCapacity))
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

        private void ReleaseUnit(UnitRequest unitRequest)
        {
            _unitLimitModel.Release(
                UnitLimitKey.From(unitRequest),
                unitRequest.FactionData.UnitCapacity);
        }

        private void ExecuteBuild(UnitRequest unitRequest)
        {
            if (unitRequest is SecondStationLevelUnitRequest ||
                unitRequest is SecondStationUnitRequest && Model.AdditionalStationsBuilt > 0 ||
                (unitRequest is LevelUnitRequest
                    ? !_productionAccess.HasHostBase
                    : !_productionAccess.CanProduce && _productionAccess.ProductionHost != Owner &&
                      unitRequest is not SecondStationUnitRequest))
            {
                if (unitRequest is ShipUnitRequest) _unitLimitModel.CancelShipOrder();
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
        }

        private Vector3 GenerateMapCoordinates(UnitRequest request)
        {
            bool found = request is DefendPlatformUnitRequest
                ? _enemyStructurePlacementService.TryGetDefensePosition(out Vector3 position)
                : _enemyStructurePlacementService.TryGetPosition(out position);
            if (found)
            {
                _enemyStructurePlacementService.RecordPlacement(request.Id, position);
                return position;
            }

            _enemyStructurePlacementService.RecordDeferred(request.Id, request.FactionData.Price);
            throw new InvalidOperationException(
                "No clear enemy structure position is available near the station or captured zones.");
        }
    }
}
