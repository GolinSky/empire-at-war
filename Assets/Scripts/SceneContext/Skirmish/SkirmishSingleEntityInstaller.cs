using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyReinforcement;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Weapon;
using LightWeightFramework.Components.Repository;
using Zenject;

public class SkirmishSingleEntityInstaller : MonoInstaller
{
    [Inject]
    private IGameModelObserver GameModelObserver { get; }
    
    [Inject]
    private IRepository Repository { get; }
    
    public override void InstallBindings()
    {
        Container.Bind<FactionType>().WithId(PlayerType.Player).FromMethod(GetPlayerFactionType);
        Container.Bind<FactionType>().WithId(PlayerType.Opponent).FromMethod(GetEnemyFactionType);
        
        
        Container
            .BindModel<FactionsModel>(Repository)
            .BindModel<WeaponDamageModel>(Repository)
            .BindModel<ProjectileModel>(Repository)
            .BindModel<LayerModel>(Repository)
            .BindModel<DamageCalculationModel>(Repository);

        Container
            .BindInterfaces<UnitRequestFactory>()
            .BindInterfaces<PurchaseMediator>()
            .BindInterfaces<EnemyPurchaseMediator>();
            //.BindInterfaces<EnemyBuildService>();

        // Container.Bind<IPurchaseMediator>().WithId(PlayerType.Player).To<PurchaseMediator>().AsSingle();
        // Container.Bind<IPurchaseMediator>().WithId(PlayerType.Opponent).To<EnemyPurchaseMediator>().AsSingle();
        
        //enemy
        ModelDependencyBuilder
            .ConstructBuilder(Container)
            .BindFromNewScriptable<EconomyModel>(Repository, PlayerType.Opponent);
        
        Container
            .Bind<IPurchaseChain>()
            .WithId(PlayerType.Opponent)
            .To<EconomyController>()
            .AsSingle();

        Container
            .Bind<IBuildShipChain>()
            .WithId(PlayerType.Opponent)
            .To<EnemyBuildService>()
            .AsSingle();
        
        ModelDependencyBuilder
            .ConstructBuilder(Container)
            .BindFromNewScriptable<EnemyFactionModel>(Repository, PlayerType.Opponent);

        Container.BindInterfaces<EnemyFactionController>(PlayerType.Opponent);
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