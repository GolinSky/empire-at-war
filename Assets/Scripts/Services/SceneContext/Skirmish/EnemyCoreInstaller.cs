using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.SceneContext.Skirmish;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class EnemyCoreInstaller : MonoInstaller
    {
        [Inject] private IRepository Repository { get; }
        [Inject] private Zenject.SceneContext SceneContext { get; }
        
        public override void InstallBindings()
        {
            Container.Install<GameUnitsInstaller>();
            
            Container.BindInterfacesExt<EnemyService>();
            Container.BindInterfacesExt<EnemyUnitCommander>();
            
            Container.BindInterfacesExt<EnemyPurchaseProcessor>();
            
            Container.BindInterfacesExt<EnemyFactionController>();
            
            
            Container.BindScriptableObject<EconomyData>(Repository);
            Container.BindInterfacesAndSelfTo<EconomyModel>().AsSingle();
            Container.BindInterfacesNonLazyExt<EconomyService>();

            
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
