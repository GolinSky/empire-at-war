using System;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.EnemyFaction.Controllers
{
   //todo: why we have here spawn logic 
    public class EnemyFactionController : Controller<EnemyFactionModel>, IBuildShipChain, IInitializable, ILateDisposable, IIncomeProvider
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly ShipFacadeFactory _shipFacadeFactory;
        private readonly IEconomyProvider _economyProvider;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly LazyInject<IMapModelObserver> _mapModel;


        private IChainHandler<UnitRequest> _nextChain;
        private readonly MiningFacilityFacade _miningFacilityFacade;
        private readonly DefendPlatformFacade _defendPlatformFacade;
        private readonly ITimerPoolWrapperService _timerPoolWrapperService;

        private PlayerType PlayerType => PlayerType.Opponent;
        public float Income => DEFAULT_INCOME;


        public EnemyFactionController(
            EnemyFactionModel model,
            ShipFacadeFactory shipFacadeFactory,
            MiningFacilityFacade miningFacilityFacade,
            DefendPlatformFacade defendPlatformFacade,
            ITimerPoolWrapperService timerPoolWrapperService, 
            IEconomyProvider economyProvider,
            IReinforcementZonesSystem reinforcementZonesSystem,
            LazyInject<IMapModelObserver> mapModel) : base(model)
        {
            _shipFacadeFactory = shipFacadeFactory;
            _miningFacilityFacade = miningFacilityFacade;
            _defendPlatformFacade = defendPlatformFacade;
            _timerPoolWrapperService = timerPoolWrapperService;
            _economyProvider = economyProvider;
            _reinforcementZonesSystem = reinforcementZonesSystem;
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
                    _timerPoolWrapperService.Invoke(() =>
                        {
                            _shipFacadeFactory.Create(PlayerType, shipUnitRequest.Key, GenerateShipCoordinates());
                        },
                        shipUnitRequest.FactionData.BuildTime);
                    break;
                }
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                {
                    _timerPoolWrapperService.Invoke(() =>
                        {
                            _miningFacilityFacade.Create(PlayerType, miningFacilityUnitRequest.Key, GenerateMapCoordinates());
                        },
                        miningFacilityUnitRequest.FactionData.BuildTime);
                    break;
                }
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                {
                    _timerPoolWrapperService.Invoke(() =>
                        {
                            _defendPlatformFacade.Create(PlayerType, defendPlatformUnitRequest.Key, GenerateMapCoordinates());
                        },
                        defendPlatformUnitRequest.FactionData.BuildTime);
                    break;
                }
                
            }
            _nextChain?.Handle(unitRequest);
        }
        
        private Vector3 GenerateShipCoordinates()
        {
            if (_reinforcementZonesSystem.TryGetRandomSpawnPosition(PlayerType, out Vector3 position))
            {
                return position;
            }

            throw new InvalidOperationException("The enemy has no owned reinforcement zone available for spawning.");
        }

        private Vector3 GenerateMapCoordinates()
        {
            Vector2Range sizeRange = _mapModel.Value.SizeRange;
            return new Vector3(
                UnityEngine.Random.Range(sizeRange.Min.x, sizeRange.Max.x),
                0f,
                UnityEngine.Random.Range(sizeRange.Min.y, sizeRange.Max.y));
        }

        public void Initialize()
        {
            _economyProvider.AddProvider(this);
        }

        public void LateDispose()
        {
            _economyProvider.AddProvider(this);
        }
    }
}
