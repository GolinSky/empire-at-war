using System;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
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

namespace EmpireAtWar.Entities.EnemyFaction.Controllers
{
   //todo: why we have here spawn logic
    public class EnemyFactionController : Controller<EnemyFactionModel>, IEnemyPurchaseProcessor, IEnemyStationCheat, IInitializable, ILateDisposable, IIncomeProvider,
        IEnemyReinforcementObserver, ITickable
    {
        private const float DEFAULT_INCOME = 15f;
        private const float DEPLOYMENT_RETRY_INTERVAL = 1f;

        private readonly IEconomyProvider _economyProvider;
        private readonly IWallet _wallet;
        private readonly IShipSpawnPoints _shipSpawnPoints;
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
        private readonly HashSet<long> _builtSquadrons = new HashSet<long>();
        private readonly MiningFacilityFactory _miningFacilityFactory;
        private readonly DefendPlatformFactory _defendPlatformFactory;
        private readonly FactionResearchModel _research;

        private bool _isInitialized;

        private PlayerId Owner => _owner.Id;
        public float Income => DEFAULT_INCOME * Model.CurrentLevel * _research.StationIncomeMultiplier;
        public bool HasPendingReinforcement => _builds.Count > 0;
        public IReadOnlyList<EnemyPendingBuild> PendingBuilds => _builds.Builds;
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
            PlayerSlot owner) : base(model)
        {
            _owner = owner;
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
            _playerRegistry.RegisterAiReinforcement(Owner, this);
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
            _playerRegistry.UnregisterAiReinforcement(Owner);
            _playerRegistry.UnregisterAiStationCheat(Owner);
            _isInitialized = false;
        }

        public bool ForceUpgradeStation()
        {
            if (_productionAccess.ProductionHost != Owner ||
                Model.GetCurrentLevelFactionData() == null || !_productionAccess.CanProduce) return false;

            Model.CurrentLevel++;
            return true;
        }

        public void Purchase(UnitRequest unitRequest)
        {
            if (!_productionAccess.CanProduce ||
                unitRequest is LevelUnitRequest && _productionAccess.ProductionHost != Owner)
            {
                return;
            }

            //todo: store reinforcement - not spawn them here
            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    if (!_wallet.TrySpend(levelUnitRequest))
                    {
                        return;
                    }

                    Model.CurrentLevel++;
                    return;
                case ShipUnitRequest:
                    if (!TryReserveAndSpend(unitRequest)) return;
                    _unitLimitModel.RecordShipOrder();
                    break;
                // A research reservation blocks a second purchase of the line until the tier completes.
                case ResearchUnitRequest:
                case SquadronUnitRequest:
                case MiningFacilityUnitRequest:
                case DefendPlatformUnitRequest:
                    if (!TryReserveAndSpend(unitRequest)) return;
                    break;
                default:
                    return;
            }

            _builds.Add(unitRequest, unitRequest.FactionData.BuildTime);
        }

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime) => _builds.Advance(deltaTime, ExecuteBuild);

        /// <summary>Puts back a saved build: its reservation is taken again and it keeps its time left.</summary>
        public void RestorePendingBuild(UnitRequest unitRequest, float timeLeft)
        {
            AdoptReservation(unitRequest);
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

        private void AdoptReservation(UnitRequest unitRequest) =>
            _unitLimitModel.Adopt(UnitLimitKey.From(unitRequest), unitRequest.FactionData.UnitCapacity);

        private void Build(UnitRequest unitRequest)
        {
            switch (unitRequest)
            {
                case ResearchUnitRequest researchUnitRequest:
                    _research.Complete(researchUnitRequest.Key);
                    ReleaseUnit(researchUnitRequest);
                    break;
                case ShipUnitRequest shipUnitRequest:
                    if (!_shipSpawnPoints.TryGetRandomSpawnPosition(
                            _productionAccess.ProductionHost, shipUnitRequest.Key, out Vector3 position))
                    {
                        if (_productionAccess.CanProduce)
                        {
                            // Keep the completed ship paid and reserved while its arrival area is blocked.
                            _builds.Add(shipUnitRequest, DEPLOYMENT_RETRY_INTERVAL);
                            return;
                        }

                        // An impossible arrival must not keep a defeated faction alive indefinitely.
                        throw new InvalidOperationException(
                            $"No open enemy spawn position is available near a held relay or home for {shipUnitRequest.Key}.");
                    }

                    Track(_shipFactory.Create(Owner, shipUnitRequest.Key, position), shipUnitRequest);
                    break;
                case SquadronUnitRequest squadronUnitRequest:
                    if (!_squadronLauncher.TryLaunchFromStation(Owner, squadronUnitRequest.Key, out ISquadron squadron))
                    {
                        // The station fell during the build, so no hangar can launch the squadron.
                        ReleaseUnit(squadronUnitRequest);
                        _wallet.Refund(squadronUnitRequest);
                        Debug.Log(
                            $"[EnemyAI:Production] {squadronUnitRequest.Id} not launched: " +
                            "no living station. Purchase refunded.");
                        return;
                    }

                    Track(squadron, squadronUnitRequest);
                    break;
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                    Track(_miningFacilityFactory.Create(Owner, miningFacilityUnitRequest.Key, GenerateMapCoordinates()),
                        miningFacilityUnitRequest);
                    break;
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                    Track(_defendPlatformFactory.Create(Owner, defendPlatformUnitRequest.Key, GenerateMapCoordinates()),
                        defendPlatformUnitRequest);
                    break;
            }
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
                return false;
            }

            if (_wallet.TrySpend(unitRequest))
            {
                return true;
            }

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
            if (_productionAccess.ProductionHost != Owner && !_productionAccess.CanProduce)
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

        private Vector3 GenerateMapCoordinates()
        {
            if (_enemyStructurePlacementService.TryGetPosition(out Vector3 position))
            {
                return position;
            }

            throw new InvalidOperationException(
                "No clear enemy structure position is available near the station or captured zones.");
        }
    }
}
