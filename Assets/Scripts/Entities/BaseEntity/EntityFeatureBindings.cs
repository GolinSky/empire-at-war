using System;
using System.Linq;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Extensions;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades.Combat;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Ship.EntityFacades.Selection;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Selection;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace EmpireAtWar.Entities.BaseEntity
{
    public static class EntityFeatureBindings
    {
        public static void RegisterHighlight(IContainerBuilder builder, EntityBindings bindings)
        {
            builder.RegisterInstance<IUnitRenderStateComponent>(bindings.Injectables.OfType<UnitRenderStateComponent>().Single());
            builder.Register<UnitHighlightFacade>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        public static void RegisterSelection(IContainerBuilder builder, EntityBindings bindings,
            SelectionType selectionType, SelectionType selectedAs)
        {
            builder.RegisterInstance(selectionType);
            builder.Register<SelectionModel>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            RegisterComponents(builder, bindings.Selection);
            builder.Register<SelectionFacade>(Lifetime.Singleton).AsImplementedInterfaces()
                .WithParameter("selectionType", selectedAs);
        }

        public static void RegisterHealth(IContainerBuilder builder, EntityBindings bindings, IHealthData baseData)
        {
            builder.RegisterInstance<IHealthData>(baseData).Keyed(EntityBindType.HealthData);
            builder.Register<ResearchHealthData>(Lifetime.Singleton).As<IHealthData>();
            builder.Register<HealthModel>(Lifetime.Singleton);
            RegisterComponents(builder, bindings.Health);
            builder.Register<HealthFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HardPointsFacade>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        public static void RegisterRadar(IContainerBuilder builder, EntityBindings bindings)
        {
            builder.Register<RadarModel>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            RegisterComponents(builder, bindings.Radar);
        }

        public static void RegisterWeapons(IContainerBuilder builder, EntityBindings bindings)
        {
            builder.Register<WeaponModel>(Lifetime.Singleton);
            RegisterComponents(builder, bindings.Weapons);
        }

        public static void RegisterCombatModifiers(IContainerBuilder builder)
        {
            builder.Register<CombatModifiers>(Lifetime.Singleton);
            builder.Register<ResearchCombatModifier>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<CombatModifiersFacade>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        public static void RegisterStationaryCombat(IContainerBuilder builder)
        {
            builder.Register<StationCombatPresenter>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<StationaryAttackFacade>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        public static void RegisterFog(IContainerBuilder builder, EntityBindings bindings, bool isHiddenByLocalFog)
        {
            if (isHiddenByLocalFog) RegisterComponents(builder, bindings.FogVisibility);
            else builder.Register<VisibleFogFacade>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        public static void RegisterStructureVision(IContainerBuilder builder)
        {
            builder.Register<FogVisionSource>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SpawnBlockerSource>(Lifetime.Singleton).AsImplementedInterfaces();
        }

        public static void RegisterComponents<T>(IContainerBuilder builder, T[] components) where T : MonoBehaviour
        {
            foreach (T component in components) RegisterComponent(builder, component);
        }

        public static void RegisterComponent<T>(IContainerBuilder builder, T component) where T : MonoBehaviour
        {
            Type[] contracts = component.GetType().GetInterfaces();
            if (component is IMonoComponent)
                contracts = contracts.Where(contract => contract != typeof(IOrderedDisposable) &&
                    contract != typeof(ILateDisposable) && contract != typeof(IDisposable)).ToArray();
            builder.RegisterComponent(component).AsSelf().As(contracts);
        }
    }
}
