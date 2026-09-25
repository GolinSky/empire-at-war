using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Patterns.ChainOfResponsibility;
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
    public class EnemyFactionController : Controller<EnemyFactionModel>, IBuildShipChain, IInitializable, ILateDisposable, IIncomeProvider,
        IEnemyReinforcementObserver
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly ShipFacadeFactory _shipFacadeFactory;
        private readonly IEconomyProvider _economyProvider;
        private readonly IPurchaseChain _purchaseChain;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly EnemyUnitLimitModel _unitLimitModel;
        private readonly ReinforcementData _reinforcementData;
        private readonly IEnemyStructurePlacementService _structurePlacement;
        private readonly IEntityLocator _entityLocator;
        private readonly ISquadronLauncher _squadronLauncher;
        private readonly Dictionary<CustomCoroutine, UnitRequest> _pendingBuilds =
            new Dictionary<CustomCoroutine, UnitRequest>();


        private IChainHandler<UnitRequest> _nextChain;
        private readonly MiningFacilityFacade _miningFacilityFacade;
        private readonly DefendPlatformFacade _defendPlatformFacade;
        private readonly TimerPoolService _timerPoolService;
        private bool _isInitialized;

        private PlayerType PlayerType => PlayerType.Opponent;
        public float Income => DEFAULT_INCOME * Model.CurrentLevel;
        public bool HasPendingReinforcement => _pendingBuilds.Count > 0;


        public EnemyFactionController(
            EnemyFactionModel model,
            ShipFacadeFactory shipFacadeFactory,
            MiningFacilityFacade miningFacilityFacade,
            DefendPlatformFacade defendPlatformFacade,
            TimerPoolService timerPoolService,
            IEconomyProvider economyProvider,
            IPurchaseChain purchaseChain,
            IReinforcementZonesSystem reinforcementZonesSystem,
            EnemyUnitLimitModel unitLimitModel,
            ReinforcementData reinforcementData,
            IEnemyStructurePlacementService structurePlacement,
            IEntityLocator entityLocator,
            ISquadronLauncher squadronLauncher) : base(model)
        {
            _shipFacadeFactory = shipFacadeFactory;
            _miningFacilityFacade = miningFacilityFacade;
            _defendPlatformFacade = defendPlatformFacade;
            _timerPoolService = timerPoolService;
            _economyProvider = economyProvider;
            _purchaseChain = purchaseChain;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _unitLimitModel = unitLimitModel;
            _reinforcementData = reinforcementData;
            _structurePlacement = structurePlacement;
            _entityLocator = entityLocator;
            _squadronLauncher = squadronLauncher;
        }
        

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            _nextChain = chainHandler;
            return _nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            if (!_entityLocator.IsStationOperational(PlayerType))
            {
                _purchaseChain.Revert(unitRequest);
                return;
            }

            //todo: store reinforcement - not spawn them here
            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    Model.CurrentLevel++;
                    _economyProvider.RecalculateIncome(this);
                  //  Debug.Log($"Upgrade level {Model.CurrentLevel}");
                    break;
                case ShipUnitRequest shipUnitRequest:
                {
                    if (!TryReserveUnit(shipUnitRequest))
                    {
                        _purchaseChain.Revert(shipUnitRequest);
                        return;
                    }

                    _unitLimitModel.RecordShipOrder();
                    ScheduleBuild(shipUnitRequest, () =>
                        {
                            ShipEntity ship = _shipFacadeFactory.Create(
                                PlayerType,
                                shipUnitRequest.Key,
                                GenerateShipCoordinates(shipUnitRequest.Key));
                            ship.OnRelease += _ => ReleaseUnit(shipUnitRequest);
                        });
                    break;
                }
                case SquadronUnitRequest squadronUnitRequest:
                {
                    if (!TryReserveUnit(squadronUnitRequest))
                    {
                        _purchaseChain.Revert(squadronUnitRequest);
                        return;
                    }

                    ScheduleBuild(squadronUnitRequest, () =>
                        {
                            ISquadron squadron = _squadronLauncher.LaunchFromStation(
                                PlayerType,
                                squadronUnitRequest.Key);
                            Action handler = null;
                            handler = () =>
                            {
                                squadron.Released -= handler;
                                ReleaseUnit(squadronUnitRequest);
                            };
                            squadron.Released += handler;
                            squadron.Hunt();
                        });
                    break;
                }
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                {
                    if (!TryReserveUnit(miningFacilityUnitRequest))
                    {
                        _purchaseChain.Revert(miningFacilityUnitRequest);
                        return;
                    }

                    ScheduleBuild(miningFacilityUnitRequest, () =>
                        {
                            Vector3 position = GenerateMapCoordinates();
                            MiningFacilityEntity facility = _miningFacilityFacade.Create(
                                PlayerType,
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
                    if (!TryReserveUnit(defendPlatformUnitRequest))
                    {
                        _purchaseChain.Revert(defendPlatformUnitRequest);
                        return;
                    }

                    ScheduleBuild(defendPlatformUnitRequest, () =>
                        {
                            Vector3 position = GenerateMapCoordinates();
                            DefendPlatformEntity platform = _defendPlatformFacade.Create(
                                PlayerType,
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
            _nextChain?.Handle(unitRequest);
        }

        private bool TryReserveUnit(UnitRequest unitRequest)
        {
            FactionData factionData = unitRequest.FactionData;
            return _unitLimitModel.TryReserve(
                GetUnitLimitId(unitRequest),
                factionData.MaxCount,
                factionData.UnitCapacity,
                _reinforcementData.MaxUnitCapacity);
        }

        private void ReleaseUnit(UnitRequest unitRequest)
        {
            _unitLimitModel.Release(
                GetUnitLimitId(unitRequest),
                unitRequest.FactionData.UnitCapacity);
        }

        private static string GetUnitLimitId(UnitRequest unitRequest)
        {
            return $"{unitRequest.GetType().FullName}:{unitRequest.Id}";
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
                _purchaseChain.Revert(unitRequest);
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
                    PlayerType,
                    shipType,
                    out Vector3 position))
            {
                return position;
            }

            throw new InvalidOperationException(
                $"No clear enemy spawn position is available in an owned zone for {shipType}.");
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

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _unitLimitModel.Reset();
            _structurePlacement.Reset();
            _economyProvider.AddProvider(this);
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
            _isInitialized = false;
        }
    }
}
