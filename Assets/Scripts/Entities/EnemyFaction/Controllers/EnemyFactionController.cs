using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Services.ReinforcementZones;
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
    public class EnemyFactionController : Controller<EnemyFactionModel>, IBuildShipChain, IInitializable, ILateDisposable, IIncomeProvider
    {
        private const float DEFAULT_INCOME = 5f;
        private const int MAX_RANDOM_SPAWN_ATTEMPTS = 100;
        private const float BASE_SPAWN_MIN_RADIUS = 20f;
        private const float BASE_SPAWN_MAX_RADIUS = 45f;

        private readonly ShipFacadeFactory _shipFacadeFactory;
        private readonly IEconomyProvider _economyProvider;
        private readonly IPurchaseChain _purchaseChain;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly EnemyUnitLimitModel _unitLimitModel;
        private readonly ReinforcementData _reinforcementData;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly Dictionary<CustomCoroutine, UnitRequest> _pendingBuilds =
            new Dictionary<CustomCoroutine, UnitRequest>();


        private IChainHandler<UnitRequest> _nextChain;
        private readonly MiningFacilityFacade _miningFacilityFacade;
        private readonly DefendPlatformFacade _defendPlatformFacade;
        private readonly ITimerPoolWrapperService _timerPoolWrapperService;
        private bool _isInitialized;

        private PlayerType PlayerType => PlayerType.Opponent;
        public float Income => DEFAULT_INCOME;


        public EnemyFactionController(
            EnemyFactionModel model,
            ShipFacadeFactory shipFacadeFactory,
            MiningFacilityFacade miningFacilityFacade,
            DefendPlatformFacade defendPlatformFacade,
            ITimerPoolWrapperService timerPoolWrapperService, 
            IEconomyProvider economyProvider,
            IPurchaseChain purchaseChain,
            IReinforcementZonesSystem reinforcementZonesSystem,
            EnemyUnitLimitModel unitLimitModel,
            ReinforcementData reinforcementData,
            LazyInject<IMapModelObserver> mapModel) : base(model)
        {
            _shipFacadeFactory = shipFacadeFactory;
            _miningFacilityFacade = miningFacilityFacade;
            _defendPlatformFacade = defendPlatformFacade;
            _timerPoolWrapperService = timerPoolWrapperService;
            _economyProvider = economyProvider;
            _purchaseChain = purchaseChain;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _unitLimitModel = unitLimitModel;
            _reinforcementData = reinforcementData;
            _mapModel = mapModel;
        }
        

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            _nextChain = chainHandler;
            return _nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            //todo: store reinforcement - not spawn them here
            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    Model.CurrentLevel++;
                  //  Debug.Log($"Upgrade level {Model.CurrentLevel}");
                    break;
                case ShipUnitRequest shipUnitRequest:
                {
                    if (!TryReserveUnit(shipUnitRequest))
                    {
                        _purchaseChain.Revert(shipUnitRequest);
                        return;
                    }

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
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                {
                    if (!TryReserveUnit(miningFacilityUnitRequest))
                    {
                        _purchaseChain.Revert(miningFacilityUnitRequest);
                        return;
                    }

                    ScheduleBuild(miningFacilityUnitRequest, () =>
                        {
                            MiningFacilityEntity facility = _miningFacilityFacade.Create(
                                PlayerType,
                                miningFacilityUnitRequest.Key,
                                GenerateMapCoordinates());
                            facility.OnRelease += () => ReleaseUnit(miningFacilityUnitRequest);
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
                            DefendPlatformEntity platform = _defendPlatformFacade.Create(
                                PlayerType,
                                defendPlatformUnitRequest.Key,
                                GenerateMapCoordinates());
                            platform.OnRelease += () => ReleaseUnit(defendPlatformUnitRequest);
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
            CustomCoroutine pendingBuild = _timerPoolWrapperService.Invoke(
                () => ExecuteBuild(unitRequest, buildAction),
                unitRequest.FactionData.BuildTime);
            _pendingBuilds.Add(pendingBuild, unitRequest);
            pendingBuild.OnFinished += HandleBuildFinished;
        }

        private void ExecuteBuild(UnitRequest unitRequest, Action buildAction)
        {
            try
            {
                buildAction();
            }
            catch (Exception exception)
            {
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

            for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
            {
                position = GeneratePositionNearBase();
                if (_reinforcementZonesSystem.IsShipSpawnPositionClear(
                        shipType,
                        position))
                {
                    return position;
                }
            }

            throw new InvalidOperationException(
                $"No clear enemy spawn position is available for {shipType}.");
        }

        private Vector3 GenerateMapCoordinates()
        {
            for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
            {
                Vector3 position = GeneratePositionNearBase();
                if (!_reinforcementZonesSystem.IsPositionInAnyZone(position))
                {
                    return position;
                }
            }

            Vector2Range sizeRange = _mapModel.Value.SizeRange;
            for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
            {
                Vector3 position = new Vector3(
                    UnityEngine.Random.Range(sizeRange.Min.x, sizeRange.Max.x),
                    0f,
                    UnityEngine.Random.Range(sizeRange.Min.y, sizeRange.Max.y));

                if (!_reinforcementZonesSystem.IsPositionInAnyZone(position))
                {
                    return position;
                }
            }

            throw new InvalidOperationException("No enemy non-ship spawn position is available outside reinforcement zones.");
        }

        private Vector3 GeneratePositionNearBase()
        {
            Vector2Range sizeRange = _mapModel.Value.SizeRange;
            Vector3 basePosition = _mapModel.Value.GetStationPosition(Model.FactionType);
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            float radius = UnityEngine.Random.Range(BASE_SPAWN_MIN_RADIUS, BASE_SPAWN_MAX_RADIUS);
            return new Vector3(
                Mathf.Clamp(basePosition.x + direction.x * radius, sizeRange.Min.x, sizeRange.Max.x),
                0f,
                Mathf.Clamp(basePosition.z + direction.y * radius, sizeRange.Min.y, sizeRange.Max.y));
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _unitLimitModel.Reset();
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
