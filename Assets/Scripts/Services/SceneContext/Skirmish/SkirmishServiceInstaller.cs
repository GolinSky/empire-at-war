using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.Health.Overlay;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.UnitDeathAnimation;
using Zenject;

namespace EmpireAtWar.SceneContext.Skirmish
{
    public class SkirmishServiceInstaller : MonoInstaller
    {
        [Inject] private IRepository Repository { get; }

        public override void InstallBindings()
        {
            
            Container.BindScriptableObject<CameraData>(Repository);
            Container.BindScriptableObject<SharedSelectionData>(Repository);
            Container.Bind<MarqueeSelectionModel>().AsSingle();
            Container
                .BindInterfacesAndSelfTo<MarqueeSelectionView>()
                .FromNewComponentOnNewGameObject()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<MarqueeSelectionPresenter>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<HealthOverlayView>()
                .FromNewComponentOnNewGameObject()
                .AsSingle();
            Container
                .BindInterfacesAndSelfTo<HealthOverlayPresenter>()
                .AsSingle()
                .NonLazy();
            Container
                .BindInterfacesAndSelfTo<CameraService>()
                .FromComponentInHierarchy()
                .AsSingle();
            Container
                .Bind<IMapObstacleContactSource>()
                .To<MapObstacle>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container
                .BindInterfacesExt<InputService>()
                .BindInterfacesExt<ShipService>()
                .BindInterfacesExt<MapObstacleContactProvider>()
                .BindInterfacesExt<ShipNavigationService>()
                .BindInterfacesExt<UnitDeathAnimationService>()
                .BindInterfacesExt<SelectionQuery>()
                .BindInterfacesExt<SelectionService>()
                .BindInterfacesExt<BattleService>();
        }
    }
}
