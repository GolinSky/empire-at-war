using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.ModelMediator;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.SceneContext;
using EmpireAtWar.Services.Enemy;
using LightWeightFramework.Components.Repository;
using Zenject;

public class SkirmishMainInstaller : MonoInstaller
{
    [Inject] private IGameModelObserver GameModelObserver { get; }

    [Inject] private IRepository Repository { get; }

    public override void InstallBindings()
    {
        //todo: use GameModelObserver.PlayerFactionType directly
        Container.Bind<FactionType>().WithId(PlayerType.Player).FromMethod(GetPlayerFactionType);
        Container.Bind<FactionType>().WithId(PlayerType.Opponent).FromMethod(GetEnemyFactionType);


        Container
            .BindModel<FactionsModel>(Repository)
            .BindModel<WeaponDamageModel>(Repository)
            .BindModel<ProjectileModel>(Repository)
            .BindModel<LayerModel>(Repository)
            .BindModel<DamageCalculationModel>(Repository);

        Container
            .BindInterfacesExt<UnitRequestFactory>()
            .BindInterfacesExt<PurchaseProcessor>();
            // .BindInterfacesExt<EnemyPurchaseMediator>();
        //.BindInterfaces<EnemyBuildService>();

        Container
            .BindInterfacesAndSelfTo<EnemyService>()
            .FromSubContainerResolve()
            .ByInstaller<EnemyCoreInstaller>()
            .AsSingle()
            .NonLazy();
        
        // Container
        //     .Bind<PlayerImpl>()
        //     .FromSubContainerResolve()
        //     .ByInstaller<PlayerCoreInstaller>()
        //     .AsSingle()
        //     .NonLazy();
        //
        //
        // Container
        //     .Bind<EnemyImpl>()
        //     .FromSubContainerResolve()
        //     .ByInstaller<EnemyCoreInstaller>()
        //     .AsSingle()
        //     .NonLazy();
        //
        //todo: move enemy to another container
        //enemy
        // ModelDependencyBuilder
        //     .ConstructBuilder(Container)
        //     .BindFromNewScriptable<EconomyModel>(Repository, PlayerType.Opponent);
        //
        // Container.BindInterfacesExt<EconomyController>();
        //
        // Container
        //     .Bind(typeof(IPurchaseChain), typeof(IEconomyProvider))
        //     .WithId(PlayerType.Opponent)
        //     .FromResolve()
        //     .AsSingle();
        //
        // ModelDependencyBuilder
        //     .ConstructBuilder(Container)
        //     .BindFromNewScriptable<EnemyFactionModel>(Repository, PlayerType.Opponent);

        // Container
        //     .BindInterfacesExt<EnemyFactionController>(PlayerType.Opponent);

        // Container
        //     .Bind<IBuildShipChain>()
        //     .WithId(PlayerType.Opponent)
        //     .FromResolve()
        //     .AsSingle();

        Container.BindInterfacesExt<ModelMediatorService>();
    }

    private FactionType GetPlayerFactionType()
    {
        return GameModelObserver.PlayerFactionType;
    }

    private FactionType GetEnemyFactionType()
    {
        return GameModelObserver.EnemyFactionType;
    }
}