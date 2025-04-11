using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Extentions;
using EmpireAtWar.MiningFacility;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Ship;
using EmpireAtWar.SpaceStation;
using EmpireAtWar.Views.DefendPlatform;
using EmpireAtWar.Views.MiningFacility;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class EnemyCoreInstaller : MonoInstaller
    {
        [Inject] private IRepository Repository { get; }
        [Inject] private Zenject.SceneContext SceneContext { get; }
        
        public override void InstallBindings()
        {
            Container
                .BindFactory<PlayerType, ShipType, Vector3, ShipView, ShipFacadeFactory>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<ShipInstaller>(GetPath<ShipInstaller>())
                .NonLazy();
    
            Container
                .BindFactory<PlayerType, FactionType, Vector3, SpaceStationView, SpaceStationViewFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<SpaceStationInstaller>(GetPath<SpaceStationInstaller>())
                .NonLazy();

            Container
                .BindFactory<PlayerType, MiningFacilityType, Vector3, MiningFacilityView, MiningFacilityFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<MiningFacilityInstaller>(GetPath<MiningFacilityInstaller>())
                .NonLazy();
            
            Container
                .BindFactory<PlayerType, DefendPlatformType, Vector3, DefendPlatformView, DefendPlatformFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab<DefendPlatformInstaller>(GetPath<DefendPlatformInstaller>())
                .NonLazy();
            
            Container.BindInterfacesExt<EnemyService>();
            
            Container.BindInterfacesExt<EnemyPurchaseProcessor>();
            
            Container.BindInterfacesExt<EnemyFactionController>();
            
            
            ModelDependencyBuilder
                .ConstructBuilder(Container)
                .BindFromNewScriptable<EconomyModel>(Repository, PlayerType.Opponent);

            Container.BindInterfacesNonLazyExt<EconomyController>();

            
            SceneContext.Container
                .Bind<IPurchaseChain>()
                .WithId(PlayerType.Opponent)
                .FromMethod(()=>Container.Resolve<IPurchaseChain>());
            
            SceneContext.Container
                .Bind<IEconomyProvider>()
                .WithId(PlayerType.Opponent)
                .FromMethod(()=>Container.Resolve<IEconomyProvider>());

            ModelDependencyBuilder
                .ConstructBuilder(Container)
                .BindFromNewScriptable<EnemyFactionModel>(Repository, PlayerType.Opponent);
            
            
            SceneContext.Container
                .Bind<IBuildShipChain>()
                .WithId(PlayerType.Opponent)
                .FromResolve()
                .AsSingle();

        }
        
        private GameObject GetPath<T>()
        {
            return Repository.Load<GameObject>(typeof(T).Name);
        }
    }
}