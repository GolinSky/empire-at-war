using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.SceneContext.Skirmish;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class EnemyCoreInstaller : MonoInstaller
    {
        [Inject] private IAssetService Repository { get; }
        [Inject] private Zenject.SceneContext SceneContext { get; }
        
        public override void InstallBindings()
        {
            Container.Install<GameUnitsInstaller>();
            
            Container.BindInterfacesExt<EnemyService>();
            Container.Bind<EnemyStrategicDecisionModel>().AsSingle();
            Container.Bind<EnemyProductionDecisionModel>().AsSingle();
            Container.Bind<EnemyStrategicContextBuilder>().AsSingle();
            Container.Bind<EnemyTaskForceExecutor>().AsSingle();
            Container.Bind<EnemyProductionStrategy>().AsSingle();
            Container.Bind<IEnemyStructurePlacementService>().To<EnemyStructurePlacementService>().AsSingle();
            Container.BindInterfacesExt<EnemyUnitCommander>();
            Container.BindInterfacesExt<EnemyShipAbilityController>();
            Container.Bind<EnemyUnitLimitModel>().AsSingle();
            Container.BindScriptableObject<ReinforcementData>(Repository);
            
            Container.BindInterfacesExt<EnemyPurchaseProcessor>();
            
            Container.BindInterfacesExt<EnemyFactionController>();
            
            
            Container.BindScriptableObject<EconomyData>(Repository);
            Container.BindInterfacesAndSelfTo<EconomyModel>().AsSingle();
            Container.BindInterfacesNonLazyExt<EconomyService>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.Bind<EnemyEconomyDebugView>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
#endif

            
            SceneContext.Container
                .Bind<IPurchaseChain>()
                .WithId(PlayerType.Opponent)
                .FromMethod(()=>Container.Resolve<IPurchaseChain>());
            
            SceneContext.Container
                .Bind<IEconomyProvider>()
                .WithId(PlayerType.Opponent)
                .FromMethod(()=>Container.Resolve<IEconomyProvider>());

            SceneContext.Container
                .Bind<IEnemyReinforcementObserver>()
                .FromMethod(()=>Container.Resolve<IEnemyReinforcementObserver>());

            ModelDependencyBuilder
                .ConstructBuilder(Container)
                .BindFromNewScriptable<EnemyFactionData>(Repository, PlayerType.Opponent);
            
            
            SceneContext.Container
                .Bind<IBuildShipChain>()
                .WithId(PlayerType.Opponent)
                .FromResolve()
                .AsSingle();

        }
    }
}
