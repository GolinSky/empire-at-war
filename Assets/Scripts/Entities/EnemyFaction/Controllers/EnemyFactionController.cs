using System;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.EconomyMediator;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.DefendPlatform;
using EmpireAtWar.Views.MiningFacility;
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

        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly LazyInject<IMapModelObserver> mapModel;
        private readonly IEconomyProvider economyProvider;


        private IChainHandler<UnitRequest> nextChain;
        private readonly MiningFacilityFacade miningFacilityFacade;
        private readonly DefendPlatformFacade defendPlatformFacade;
        private readonly ITimerPoolWrapperService timerPoolWrapperService;

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
            this.shipFacadeFactory = shipFacadeFactory;
            this.miningFacilityFacade = miningFacilityFacade;
            this.defendPlatformFacade = defendPlatformFacade;
            this.timerPoolWrapperService = timerPoolWrapperService;
            this.mapModel = mapModel;
            this.economyProvider = economyProvider;
        }
        

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
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
                    timerPoolWrapperService.Invoke(() =>
                        {
                            shipFacadeFactory.Create(PlayerType, shipUnitRequest.Key, GenerateShipCoordinates());
                        },
                        shipUnitRequest.FactionData.BuildTime);
                    break;
                }
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                {
                    timerPoolWrapperService.Invoke(() =>
                        {
                            miningFacilityFacade.Create(PlayerType, miningFacilityUnitRequest.Key, GenerateShipCoordinates());
                        },
                        miningFacilityUnitRequest.FactionData.BuildTime);
                    break;
                }
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                {
                    timerPoolWrapperService.Invoke(() =>
                        {
                            defendPlatformFacade.Create(PlayerType, defendPlatformUnitRequest.Key, GenerateShipCoordinates());
                        },
                        defendPlatformUnitRequest.FactionData.BuildTime);
                    break;
                }
                
            }
            nextChain?.Handle(unitRequest);
        }
        
        private Vector3 GenerateShipCoordinates()
        {
            Vector3 minRange = mapModel.Value.SizeRange.Min;
            Vector3 maxRange = mapModel.Value.SizeRange.Max;
            Random.InitState((int)DateTime.Now.Ticks);

            Vector3 vector3 = new Vector3(Random.Range(minRange.x, maxRange.x), 
                0f,
                Random.Range(minRange.y, maxRange.y));
         //   Debug.Log($"GenerateShipCoordinates:{vector3}");
            return vector3;
        }

        public void Initialize()
        {
            economyProvider.AddProvider(this);
        }

        public void LateDispose()
        {
            economyProvider.AddProvider(this);
        }
    }
}