using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.Health.Overlay;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.ViewComponents.Health;
using Zenject;
using UnityEngine;
using EmpireAtWar.ViewComponents.Weapon;
using EmpireAtWar.Utils;

namespace EmpireAtWar.SceneContext.Skirmish
{
    public class SkirmishServiceInstaller : MonoInstaller
    {
        [SerializeField] private ImpactEffectView impactEffectPrefab;
        [SerializeField] private UnitExplosionView unitExplosionPrefab;
        [SerializeField] private Material rangeDebugLineMaterial;
        [SerializeField] private ShipSfxData shipSfxData;
        [SerializeField] private ShipSfxSources shipSfxSourcesPrefab;

        [Inject] private IAssetService AssetService { get; }

        public override void InstallBindings()
        {
            Container.BindInstance(unitExplosionPrefab);
            Container.Bind<ShipSfxData>().FromInstance(shipSfxData).AsSingle();
            Container.Bind<ShipSfxSources>()
                .FromComponentInNewPrefab(shipSfxSourcesPrefab)
                .UnderTransform(transform)
                .AsSingle();
            Container.BindInterfacesAndSelfTo<ShipSfxService>().AsSingle().NonLazy();
            Container.BindTickableExecutionOrder<ShipSfxService>(1000);
            Container.BindDisposableExecutionOrder<ShipSfxService>(-1000);
            Container.Bind<IImpactEffectView>().To<ImpactEffectView>()
                .FromComponentInNewPrefab(impactEffectPrefab).AsSingle().NonLazy();
            Container.Bind<ImpactEffectPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<CombatAttackCoordinator>().AsSingle().NonLazy();
            Container.BindLateTickableExecutionOrder<CombatAttackCoordinator>(-1000);
            Container.BindInterfacesAndSelfTo<RangeDebugModel>().AsSingle();
            Container.Bind<DebugRangeCircleFactory>().AsSingle().WithArguments(rangeDebugLineMaterial);
            
            Container.BindScriptableObject<CameraData>(AssetService);
            Container.BindScriptableObject<CinematicCameraData>(AssetService);
            Container.BindScriptableObject<SharedSelectionData>(AssetService);
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
                .BindInterfacesExt<UiHitTest>()
                .BindInterfacesExt<InputLockService>()
                .BindInterfacesExt<PointerInput>()
                .BindInterfacesExt<PointerGestures>()
                .BindInterfacesExt<CameraInput>()
                .BindInterfacesExt<SelectionInput>()
                .BindInterfacesExt<UnitOrderInput>()
                .BindInterfacesExt<ShipService>()
                .BindInterfacesExt<MapObstacleContactProvider>()
                .BindInterfacesExt<ShipNavigationService>()
                .BindInterfacesExt<UnitWreckService>()
                .BindInterfacesExt<UnitExplosionService>()
                .BindInterfacesExt<SelectionQuery>()
                .BindInterfacesExt<SelectionService>();
        }
    }
}
