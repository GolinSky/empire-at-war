using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.Health.Overlay;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Ship;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.UnitDeathAnimation;
using Zenject;
using UnityEngine;
using EmpireAtWar.ViewComponents.Weapon;

namespace EmpireAtWar.SceneContext.Skirmish
{
    public class SkirmishServiceInstaller : MonoInstaller
    {
        [SerializeField] private ImpactEffectView impactEffectPrefab;
        [Inject] private IAssetService Repository { get; }

        public override void InstallBindings()
        {
            Container.Bind<IImpactEffectView>().To<ImpactEffectView>()
                .FromComponentInNewPrefab(impactEffectPrefab).AsSingle().NonLazy();
            Container.Bind<ImpactEffectPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<CombatAttackCoordinator>().AsSingle().NonLazy();
            Container.BindLateTickableExecutionOrder<CombatAttackCoordinator>(-1000);
            
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
                .Bind(typeof(IMapObstacleContactSource), typeof(IMiniMapObstacleSource))
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
                .BindInterfacesExt<SelectionService>();
        }
    }
}
