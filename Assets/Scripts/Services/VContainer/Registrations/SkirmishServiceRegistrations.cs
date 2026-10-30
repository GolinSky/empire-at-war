using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Health.Overlay;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Radar;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.Ship;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public static class SkirmishServiceRegistrations
    {
        public static void Register(IContainerBuilder builder, IAssetService assets, Transform owner,
            ImpactEffectView impactEffectPrefab, UnitExplosionView unitExplosionPrefab,
            Material rangeDebugLineMaterial, ShipSfxData shipSfxData, InterfaceSfxData interfaceSfxData,
            ShipAmbienceData shipAmbienceData, ShipSfxSources shipSfxSourcesPrefab,
            CameraService cameraService)
        {
            builder.RegisterInstance(unitExplosionPrefab);
            builder.RegisterInstance(shipSfxData);
            builder.RegisterInstance(interfaceSfxData);
            builder.RegisterInstance(shipAmbienceData);
            ShipSfxSources sources = Object.Instantiate(shipSfxSourcesPrefab, owner);
            builder.RegisterComponent(sources);
            builder.Register<ShipSfxService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<InterfaceSfxService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipAmbienceService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            ImpactEffectView impactView = Object.Instantiate(impactEffectPrefab, owner);
            builder.RegisterComponent(impactView).AsImplementedInterfaces().AsSelf();
            builder.Register<ImpactEffectPresenter>(Lifetime.Singleton);
            builder.Register<BoltFlightBatch>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShotPoolPrewarmer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FighterPoseBatch>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SquadronOrderScheduler>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SquadronTargetIndex>(Lifetime.Singleton);
            builder.Register<CombatAttackCoordinator>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<IncomingMissileRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<RangeDebugModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<DebugRangeCircleFactory>(Lifetime.Singleton).WithParameter(rangeDebugLineMaterial);
            AssetRegistrations.RegisterShared<CameraData>(builder, assets);
            AssetRegistrations.RegisterShared<CinematicCameraData>(builder, assets);
            AssetRegistrations.RegisterShared<SharedSelectionData>(builder, assets);
            builder.Register<MarqueeSelectionModel>(Lifetime.Singleton);
            GameObject marqueeRoot = new GameObject(nameof(MarqueeSelectionView));
            marqueeRoot.transform.SetParent(owner, false);
            builder.RegisterComponent(marqueeRoot.AddComponent<MarqueeSelectionView>())
                .AsImplementedInterfaces().AsSelf();
            builder.Register<MarqueeSelectionPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            GameObject healthRoot = new GameObject(nameof(HealthOverlayView));
            healthRoot.transform.SetParent(owner, false);
            builder.RegisterComponent(healthRoot.AddComponent<HealthOverlayView>())
                .AsImplementedInterfaces().AsSelf();
            builder.Register<HealthOverlayPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(cameraService).AsImplementedInterfaces().AsSelf();
            GameObject iconRoot = new GameObject(nameof(SquadronIconLayer));
            iconRoot.transform.SetParent(owner, false);
            builder.RegisterComponent(iconRoot.AddComponent<SquadronIconLayer>())
                .AsImplementedInterfaces().AsSelf();
            builder.Register<UiHitTest>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<InputLockService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<PointerInput>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<PointerGestures>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CameraInput>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SelectionInput>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UnitOrderInput>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EmpireAtWar.Services.FleetCommand.FleetCommandService>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<MapObstacleContactProvider>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HazardService>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<HazardEffectsPresenter>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<ShipNavigationService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<RadarScanService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UnitWreckService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UnitExplosionService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SelectionQuery>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SelectionService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
