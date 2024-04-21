using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Patterns.Strategy;
using EmpireAtWar.Views.Ship;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

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

        public EnemyFactionController(
            EnemyFactionModel model,
            ShipFacadeFactory shipFacadeFactory,
            SpaceStationViewFacade spaceStationViewFacade,
            LazyInject<IMapModelObserver> mapModel,
            LazyInject<IEnemyPurchaseMediator> purchaseMediator,
            IUnitRequestFactory unitRequestFactory) : base(model)
        {
            this.shipFacadeFactory = shipFacadeFactory;
            this.spaceStationViewFacade = spaceStationViewFacade;
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
            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    Model.CurrentLevel++;
                    break;
                
            }
            nextChain.Handle(unitRequest);
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