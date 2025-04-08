using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Enemy;
using LightWeightFramework.Components.Repository;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class EnemyCoreInstaller : Installer
    {
        [Inject] private IRepository Repository { get; }
        [Inject] private Zenject.SceneContext SceneContext { get; }
        
        public override void InstallBindings()
        {
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
    }
}