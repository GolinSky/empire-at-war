using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Controllers.ShipUi;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using UnityEngine;
using ViewComponents;
using Zenject;

public class SkirmishMainInstaller : MonoInstaller
{
    [SerializeField] private FogOfWarSystem fogOfWarSystem;
    [SerializeField] private ReinforcementZoneData reinforcementZoneData;
    [Inject] private IGameModelObserver GameModelObserver { get; }
    [Inject] private IRepository Repository { get; }

    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<ReinforcementZonesSystem>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container.Bind<ReinforcementZoneData>().FromInstance(reinforcementZoneData).AsSingle();

        Container.BindInterfacesExt<AttackDataFactory>();
        Container.Bind<BattleVictoryModel>().AsSingle();
        Container.BindInterfacesExt<BattleVictoryService>();

        Container
            .BindInterfacesAndSelfTo<UiService>()
            .FromComponentInNewPrefab(Repository.LoadPrefab(nameof(UiService)))
            .AsSingle()
            .NonLazy();
        Container
            .BindFactory<UiType, Transform, BaseUi, UiFacade>()
            .FromSubContainerResolve()
            .ByNewGameObjectInstaller<UiInstaller>();

        Container.BindInterfacesExt<EntityLocator>();
        
        
        //todo: use GameModelObserver.PlayerFactionType directly
        Container.Bind<FactionType>().WithId(PlayerType.Player).FromMethod(GetPlayerFactionType);
        Container.Bind<FactionType>().WithId(PlayerType.Opponent).FromMethod(GetEnemyFactionType);
        
        Container.BindModel<MenuModel>(Repository);
        Container.BindInterfacesNonLazyExt<MenuController>();
        
        Container.BindModel<ShipUiModel>(Repository);
        Container.BindInterfacesNonLazyExt<ShipUiController>();
        
        //todo: merge map model with minimap 
        Container.BindModel<MapModel>(Repository);
        Container.BindModel<MiniMapModel>(Repository);
        Container.BindInterfacesNonLazyExt<MiniMapController>();
        
        Container.BindModel<CoreGameModel>(Repository);
        Container.BindInterfacesNonLazyExt<SkirmishOrhestrator>();
        
        Container
            .BindModel<FactionsModel>(Repository)
            .BindModel<WeaponDamageModel>(Repository)
            .BindModel<ProjectileModel>(Repository)
            .BindModel<LayerModel>(Repository)
            .BindModel<DamageCalculationModel>(Repository);
        Container.BindInterfacesAndSelfTo<LayerService>().AsSingle();

        Container.BindScriptableObject<ShipsData>(Repository);

        Container
            .BindInterfacesExt<UnitRequestFactory>();

        Container.BindEntityExt(fogOfWarSystem);

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
