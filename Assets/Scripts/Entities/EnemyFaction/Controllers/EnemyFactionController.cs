using System;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Ship;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Entities.EnemyFaction.Controllers
{
   //todo: why we have here spawn logic 
    public class EnemyFactionController : Controller<EnemyFactionModel>, IBuildShipChain, IInitializable, ILateDisposable, IIncomeProvider
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly ShipFacadeFactory _shipFacadeFactory;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly IEconomyProvider _economyProvider;


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
            LazyInject<IMapModelObserver> mapModel,
            IEconomyProvider economyProvider) : base(model)
        {
            _shipFacadeFactory = shipFacadeFactory;
            _miningFacilityFacade = miningFacilityFacade;
            _defendPlatformFacade = defendPlatformFacade;
            _timerPoolWrapperService = timerPoolWrapperService;
            _mapModel = mapModel;
            _economyProvider = economyProvider;
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
                            _miningFacilityFacade.Create(PlayerType, miningFacilityUnitRequest.Key, GenerateShipCoordinates());
                        },
                        miningFacilityUnitRequest.FactionData.BuildTime);
                    break;
                }
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                {
                    _timerPoolWrapperService.Invoke(() =>
                        {
                            _defendPlatformFacade.Create(PlayerType, defendPlatformUnitRequest.Key, GenerateShipCoordinates());
                        },
                        defendPlatformUnitRequest.FactionData.BuildTime);
                    break;
                }
                
            }
            _nextChain?.Handle(unitRequest);
        }
        
        private Vector3 GenerateShipCoordinates()
        {
            Vector3 minRange = _mapModel.Value.SizeRange.Min;
            Vector3 maxRange = _mapModel.Value.SizeRange.Max;
            Random.InitState((int)DateTime.Now.Ticks);

            Vector3 vector3 = new Vector3(Random.Range(minRange.x, maxRange.x), 
                0f,
                Random.Range(minRange.y, maxRange.y));
         //   Debug.Log($"GenerateShipCoordinates:{vector3}");
            return vector3;
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