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
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Entities.EnemyFaction.Controllers
{
   //todo: why we have here spawn logic
    public class EnemyFactionController : Controller<EnemyFactionModel>, IEnemyPurchaseProcessor, IInitializable, ILateDisposable, IIncomeProvider,
        IEnemyReinforcementObserver
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly IEconomyProvider _economyProvider;
        private readonly IWallet _wallet;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly IEnemyStructurePlacementService _structurePlacement;
        private readonly IStationRegistry _stationRegistry;
        private readonly ISquadronLauncher _squadronLauncher;
        private readonly IEnemySquadronCommander _squadronCommander;
        private readonly IPlayerRegistry _playerRegistry;

        private readonly ShipFactory _shipFactory;
        private readonly EnemyUnitLimitModel _unitLimitModel;
        private readonly ReinforcementData _reinforcementData;
        private readonly PlayerSlot _owner;
        private readonly Dictionary<CustomCoroutine, UnitRequest> _pendingBuilds = new();
        private readonly MiningFacilityFactory _miningFacilityFactory;
        private readonly DefendPlatformFactory _defendPlatformFactory;
        private readonly TimerPoolService _timerPoolService;

        private bool _isInitialized;

        private PlayerId Owner => _owner.Id;
        public float Income => DEFAULT_INCOME * Model.CurrentLevel;
        public bool HasPendingReinforcement => _pendingBuilds.Count > 0;

        public EnemyFactionController(
            IEconomyProvider economyProvider,
            IWallet wallet,
            IReinforcementZonesSystem reinforcementZonesSystem,
            IEnemyStructurePlacementService structurePlacement,
            IStationRegistry stationRegistry,
            ISquadronLauncher squadronLauncher,
            IEnemySquadronCommander squadronCommander,
            IPlayerRegistry playerRegistry,
            EnemyFactionModel model,
            ShipFactory shipFactory,
            MiningFacilityFactory miningFacilityFactory,
            DefendPlatformFactory defendPlatformFactory,
            TimerPoolService timerPoolService,
            EnemyUnitLimitModel unitLimitModel,
            ReinforcementData reinforcementData,
            PlayerSlot owner) : base(model)
        {
            _owner = owner;
            _playerRegistry = playerRegistry;
            _shipFactory = shipFactory;
            _miningFacilityFactory = miningFacilityFactory;
            _defendPlatformFactory = defendPlatformFactory;
            _timerPoolService = timerPoolService;
            _economyProvider = economyProvider;
            _wallet = wallet;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _unitLimitModel = unitLimitModel;
            _reinforcementData = reinforcementData;
            _structurePlacement = structurePlacement;
            _stationRegistry = stationRegistry;
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
            _structurePlacement.Reset();
            _economyProvider.AddProvider(this);
            _playerRegistry.RegisterAiReinforcement(Owner, this);
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
            _playerRegistry.UnregisterAiReinforcement(Owner);
            _isInitialized = false;
        }

        public void Purchase(UnitRequest unitRequest)
        {
            if (!_stationRegistry.IsStationOperational(Owner))
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
                    _economyProvider.RecalculateIncome(this);
                  //  Debug.Log($"Upgrade level {Model.CurrentLevel}");
                    break;
                case ShipUnitRequest shipUnitRequest:
                {
                    if (!TryReserveAndSpend(shipUnitRequest))
                    {
                        return;
                    }

                    _unitLimitModel.RecordShipOrder();
                    ScheduleBuild(shipUnitRequest, () =>
                        {
                            ShipEntity ship = _shipFactory.Create(
                                Owner,
                                shipUnitRequest.Key,
                                GenerateShipCoordinates(shipUnitRequest.Key));
                            ship.OnRelease += _ => ReleaseUnit(shipUnitRequest);
                        });
                    break;
                }
                case SquadronUnitRequest squadronUnitRequest:
                {
                    if (!TryReserveAndSpend(squadronUnitRequest))
                    {
                        return;
                    }

                    ScheduleBuild(squadronUnitRequest, () =>
                        {
                            ISquadron squadron = _squadronLauncher.LaunchFromStation(
                                Owner,
                                squadronUnitRequest.Key);
                            Action handler = null;
                            handler = () =>
                            {
                                squadron.Released -= handler;
                                ReleaseUnit(squadronUnitRequest);
                            };
                            squadron.Released += handler;
                            _squadronCommander.Command(squadron);
                        });
                    break;
                }
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                {
                    if (!TryReserveAndSpend(miningFacilityUnitRequest))
                    {
                        return;
                    }

                    ScheduleBuild(miningFacilityUnitRequest, () =>
                        {
                            Vector3 position = GenerateMapCoordinates();
                            MiningFacilityEntity facility = _miningFacilityFactory.Create(
                                Owner,
                                miningFacilityUnitRequest.Key,
                                position);
                            facility.OnRelease += () =>
                            {
                                _structurePlacement.RecordDestroyedPosition(
                                    facility.transform.position);
                                ReleaseUnit(miningFacilityUnitRequest);
                            };
                        });
                    break;
                }
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                {
                    if (!TryReserveAndSpend(defendPlatformUnitRequest))
                    {
                        return;
                    }

                    ScheduleBuild(defendPlatformUnitRequest, () =>
                        {
                            Vector3 position = GenerateMapCoordinates();
                            DefendPlatformEntity platform = _defendPlatformFactory.Create(
                                Owner,
                                defendPlatformUnitRequest.Key,
                                position);
                            platform.OnRelease += () =>
                            {
                                _structurePlacement.RecordDestroyedPosition(
                                    platform.transform.position);
                                ReleaseUnit(defendPlatformUnitRequest);
                            };
                        });
                    break;
                }

            }
        }

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

        private void ReleaseUnit(UnitRequest unitRequest)
        {
            _unitLimitModel.Release(
                UnitLimitKey.From(unitRequest),
                unitRequest.FactionData.UnitCapacity);
        }

        private void ScheduleBuild(UnitRequest unitRequest, Action buildAction)
        {
            CustomCoroutine pendingBuild = _timerPoolService.Invoke(
                () => ExecuteBuild(unitRequest, buildAction),
                unitRequest.FactionData.BuildTime);
            _pendingBuilds.Add(pendingBuild, unitRequest);
            pendingBuild.OnFinished += HandleBuildFinished;
        }

        private void ExecuteBuild(UnitRequest unitRequest, Action buildAction)
        {
            // Queued reinforcements still arrive after the station falls so the fleet victory can resolve.
            try
            {
                buildAction();
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

        private void HandleBuildFinished(CustomCoroutine pendingBuild)
        {
            pendingBuild.OnFinished -= HandleBuildFinished;
            _pendingBuilds.Remove(pendingBuild);
        }

        private void CancelPendingBuilds()
        {
            foreach (KeyValuePair<CustomCoroutine, UnitRequest> pendingBuild
                     in _pendingBuilds)
            {
                pendingBuild.Key.OnFinished -= HandleBuildFinished;
                pendingBuild.Key.Release();
                if (pendingBuild.Value is ShipUnitRequest)
                {
                    _unitLimitModel.CancelShipOrder();
                }
                ReleaseUnit(pendingBuild.Value);
            }

            _pendingBuilds.Clear();
        }

        private Vector3 GenerateShipCoordinates(ShipType shipType)
        {
            if (_reinforcementZonesSystem.TryGetRandomSpawnPosition(
                    Owner,
                    shipType,
                    out Vector3 position))
            {
                return position;
            }

            throw new InvalidOperationException(
                $"No clear enemy spawn position is available in an allied zone for {shipType}.");
        }

        private Vector3 GenerateMapCoordinates()
        {
            if (_structurePlacement.TryGetPosition(out Vector3 position))
            {
                return position;
            }

            throw new InvalidOperationException(
                "No clear enemy structure position is available near the station or captured zones.");
        }
    }
}
