using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades.Combat;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Ship.EntityFacades.Selection;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Selection;
using Zenject;

namespace EmpireAtWar.Entities.BaseEntity
{
    /// <summary>Binding groups shared by dynamic entity installers; plain static calls, no installer allocation.</summary>
    public static class EntityFeatureBindings
    {
        private const int HEALTH_INITIALIZE_ORDER = -100;

        public static DiContainer BindSelectionFeature(this DiContainer container, SelectionType selectionType)
        {
            container.BindEntityExt(selectionType);
            container.Bind<SelectionModel>().AsSingle();
            container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
            container.BindInterfacesAndSelfTo<SelectionComponent>().FromComponentsInHierarchy().AsCached();
            container.BindInterfacesExt<SelectionFacade>();
            return container;
        }

        public static DiContainer BindHealthFeature(this DiContainer container)
        {
            container.BindInitializableExecutionOrder<HealthComponent>(HEALTH_INITIALIZE_ORDER);
            container.Bind<HealthModel>().AsSingle();
            container.BindInterfacesAndSelfTo<HealthComponent>().FromComponentsInHierarchy().AsCached();
            container
                .BindInterfacesExt<HealthFacade>()
                .BindInterfacesExt<HardPointsFacade>();
            return container;
        }

        public static DiContainer BindRadarFeature(this DiContainer container)
        {
            container.Bind<RadarModel>().AsSingle();
            container.Bind<IRadarModelObserver>().To<RadarModel>().FromResolve();
            container.BindInterfacesAndSelfTo<RadarComponent>().FromComponentsInHierarchy().AsCached();
            return container;
        }

        public static DiContainer BindWeaponFeature(this DiContainer container)
        {
            container.Bind<WeaponModel>()
                .FromMethod(context => new WeaponModel(context.Container.Resolve<DamageMatrixData>().CopyAccuracy()))
                .AsSingle();
            container.BindInterfacesAndSelfTo<WeaponComponent>().FromComponentsInHierarchy().AsCached();
            return container;
        }

        public static DiContainer BindCombatModifiersFeature(this DiContainer container)
        {
            container.Bind<CombatModifiers>().AsSingle();
            container.BindInterfacesTo<ResearchCombatModifier>().AsSingle();
            container.Decorate<IHealthData>().With<ResearchHealthData>();
            container.BindInterfacesExt<CombatModifiersFacade>();
            return container;
        }

        public static DiContainer BindStationaryCombatFeature(this DiContainer container)
        {
            container.BindInterfacesAndSelfTo<StationCombatPresenter>().AsSingle();
            container.BindInterfacesExt<StationaryAttackFacade>();
            return container;
        }

        /// <param name="isHiddenByLocalFog">True for units outside the local team; they disappear under the local fog of war.</param>
        public static DiContainer BindFogOfWarFeature(this DiContainer container, bool isHiddenByLocalFog)
        {
            if (isHiddenByLocalFog)
                container.BindInterfacesAndSelfTo<FogVisibilityComponent>().FromComponentsInHierarchy().AsCached();
            return container;
        }

        /// <summary>Every structure gives its owner's team vision around it.</summary>
        public static DiContainer BindFogVisionFeature(this DiContainer container)
        {
            container.BindInterfacesTo<FogVisionSource>().AsSingle();
            return container;
        }

        /// <summary>Every structure keeps hostile reinforcements out of its spawn-block radius.</summary>
        public static DiContainer BindSpawnBlockerFeature(this DiContainer container)
        {
            container.BindInterfacesTo<SpawnBlockerSource>().AsSingle();
            return container;
        }
    }
}
