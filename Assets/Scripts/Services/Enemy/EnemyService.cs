using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Patterns.Strategy;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Components.Service;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyService : IService
    {
    }

    public class EnemyService : Service, IInitializable, IEnemyService, ITickable
    {
        
        private IUnitSpawnStrategy currentStrategy;
        private Vector3 stationPosition;
        private SpaceStationView spaceStationView;
        private readonly SpaceStationViewFacade spaceStationViewFacade;
        private readonly EnemyPurchaseProcessor enemyPurchaseProcessor;
        private readonly LazyInject<IMapModelObserver> mapModel;
        private readonly IUnitRequestFactory unitRequestFactory;
        private readonly EnemyFactionModel enemyFactionModel;

        [Inject(Id = PlayerType.Opponent)]
        public FactionType FactionType { get; }

        public EnemyService(
            LazyInject<IMapModelObserver> mapModel,
            SpaceStationViewFacade spaceStationViewFacade,
            EnemyPurchaseProcessor enemyPurchaseProcessor, 
            IUnitRequestFactory unitRequestFactory,
            EnemyFactionModel enemyFactionModel)
        {
            this.mapModel = mapModel;
            this.spaceStationViewFacade = spaceStationViewFacade;
            this.enemyPurchaseProcessor = enemyPurchaseProcessor;
            this.unitRequestFactory = unitRequestFactory;
            this.enemyFactionModel = enemyFactionModel;
        }
        
        public void Initialize()
        {
            SetStrategy(UnitSpawnStrategyType.LevelUpFast);
            stationPosition = mapModel.Value.GetStationPosition(PlayerType.Opponent);
            spaceStationView = spaceStationViewFacade.Create(PlayerType.Opponent, FactionType,  stationPosition);
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
            //todo: refactor this - get rid of new - use factory
            currentStrategy = new TempStrategy(enemyFactionModel, enemyPurchaseProcessor, unitRequestFactory);
            currentStrategy.Start();
        }
    }
}