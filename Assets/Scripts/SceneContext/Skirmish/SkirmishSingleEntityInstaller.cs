using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Extentions;
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

        Container.BindInterfaces<UnitRequestFactory>();
        Container.BindInterfaces<PurchaseMediator>();
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