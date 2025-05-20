using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Controllers.ShipUi;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Ui.Base;
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
        Container.BindInterfacesExt<AttackDataFactory>();
        
        Container.BindInterfacesAndSelfTo<LocationService>().FromInstance(locationService).AsSingle();
        Container.BindInterfacesExt<EntityLocator>();
        
        
        //todo: use GameModelObserver.PlayerFactionType directly
        Container.Bind<FactionType>().WithId(PlayerType.Player).FromMethod(GetPlayerFactionType);
        Container.Bind<FactionType>().WithId(PlayerType.Opponent).FromMethod(GetEnemyFactionType);
        
        Container.BindInterfacesExt<UiService>();
        

        Container.BindModel<MenuModel>(Repository);
        Container.BindInterfacesNonLazyExt<MenuController>();
        
        Container.BindModel<ShipUiModel>(Repository);
        Container.BindInterfacesNonLazyExt<ShipUiController>();
        
        //todo: merge map model with minimap 
        Container.BindModel<MapModel>(Repository);
        Container.BindModel<MiniMapModel>(Repository);
        Container.BindInterfacesNonLazyExt<MiniMapController>();
        
        Container.BindModel<CoreGameModel>(Repository);
        Container.BindInterfacesNonLazyExt<CoreGameController>();
        
        Container
            .BindModel<FactionsModel>(Repository)
            .BindModel<WeaponDamageModel>(Repository)
            .BindModel<ProjectileModel>(Repository)
            .BindModel<LayerModel>(Repository)
            .BindModel<DamageCalculationModel>(Repository);

        Container
            .BindInterfacesExt<UnitRequestFactory>();

        Container
            .BindFactory<UiType, Transform, BaseUi, UiFacade>()
            .FromSubContainerResolve()
            .ByNewGameObjectInstaller<UiInstaller>();

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