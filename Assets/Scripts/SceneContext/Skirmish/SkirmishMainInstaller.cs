using EmpireAtWar;
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
using EmpireAtWar.Services.Player;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;
using LocationService = EmpireAtWar.Services.Location.LocationService;

public class SkirmishMainInstaller : MonoInstaller
{
    [SerializeField] private LocationService locationService;
    
    [Inject] private IGameModelObserver GameModelObserver { get; }
    [Inject] private IRepository Repository { get; }

    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<LocationService>().FromInstance(locationService).AsSingle();
        
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
            .BindInterfacesExt<UnitRequestFactory>();

        // Container
        //     .BindInterfacesAndSelfTo<EnemyService>()
        //     .FromSubContainerResolve()
        //     .ByInstaller<EnemyCoreInstaller>()
        //     .AsSingle()
        //     .NonLazy();
        
        // Container
        //     .BindInterfacesAndSelfTo<PlayerService>()
        //     .FromSubContainerResolve()
        //     .ByInstaller<PlayerCoreInstaller>()
        //     .AsSingle()
        //     .NonLazy();


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