using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Patterns.Strategy;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Views.DefendPlatform;
using EmpireAtWar.Views.MiningFacility;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Entities.EnemyFaction.Controllers
{
    public interface IEnemyShipSpawner
    {
        void SetStrategy(UnitSpawnStrategyType strategyType);
    }
    
    public class EnemyFactionController : Controller<EnemyFactionModel>, IInitializable, ILateDisposable, IEnemyShipSpawner, IBuildShipChain, ITickable
    {
        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly SpaceStationViewFacade spaceStationViewFacade;
        private readonly LazyInject<IMapModelObserver> mapModel;
        private readonly LazyInject<IEnemyPurchaseMediator> purchaseMediator;
        private readonly IUnitRequestFactory unitRequestFactory;

        private Vector3 stationPosition;
        private SpaceStationView spaceStationView;
        private IChainHandler<UnitRequest> nextChain;
        private IUnitSpawnStrategy currentStrategy;
        private readonly MiningFacilityFacade miningFacilityFacade;
        private readonly DefendPlatformFacade defendPlatformFacade;
        private readonly ITimerPoolWrapperService timerPoolWrapperService;

        private PlayerType PlayerType => PlayerType.Opponent;

        public EnemyFactionController(
            EnemyFactionModel model,
            SpaceStationViewFacade spaceStationViewFacade,
            ShipFacadeFactory shipFacadeFactory,
            MiningFacilityFacade miningFacilityFacade,
            DefendPlatformFacade defendPlatformFacade,
            ITimerPoolWrapperService timerPoolWrapperService, 
            LazyInject<IMapModelObserver> mapModel,
            LazyInject<IEnemyPurchaseMediator> purchaseMediator,
            IUnitRequestFactory unitRequestFactory) : base(model)
        {
            this.shipFacadeFactory = shipFacadeFactory;
            this.spaceStationViewFacade = spaceStationViewFacade;
            this.miningFacilityFacade = miningFacilityFacade;
            this.defendPlatformFacade = defendPlatformFacade;
            this.timerPoolWrapperService = timerPoolWrapperService;
            this.mapModel = mapModel;
            this.purchaseMediator = purchaseMediator;
            this.unitRequestFactory = unitRequestFactory;
        }

        public void Initialize()
        {
            stationPosition = mapModel.Value.GetStationPosition(PlayerType.Opponent);
            spaceStationView = spaceStationViewFacade.Create(PlayerType.Opponent, Model.FactionType,  stationPosition);
        }

        public void LateDispose()
        {
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

        public void Tick()
        {
            currentStrategy?.Update();
        }

        public void SetStrategy(UnitSpawnStrategyType strategyType)
        {
            if (currentStrategy != null)
            {
                currentStrategy.Stop();
            }
            currentStrategy = new TempStrategy(Model, purchaseMediator.Value, unitRequestFactory);
            currentStrategy.Start();
        }

    }
}