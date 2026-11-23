using System.Linq;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Ship
{
    public sealed class ShipInstaller : DynamicEntityInstaller<Ship, ShipData>
    {
        private readonly ShipType _shipType;
        private readonly ShipSpawnPose _pose;
        private readonly FactionType _shipFaction;

        public ShipInstaller(Ship view, ShipData data, ILocalPlayer localPlayer, PlayerId owner,
            long entityId, Vector3 position, ShipType shipType, ShipSpawnPose pose, FactionType shipFaction)
            : base(view, data, localPlayer, owner, entityId, position)
        {
            _shipType = shipType;
            _pose = pose;
            _shipFaction = shipFaction;
        }

        protected override void InstallFeatures(IContainerBuilder builder)
        {
            builder.RegisterInstance(_shipType);
            builder.RegisterInstance(_pose);
            builder.RegisterInstance(Data).AsSelf().As<IShipData, IShipMoveData, IRadarData, IHangarData>();
            builder.Register(resolver => new ShipVisionFootprint(View.transform,
                    resolver.Resolve<long>(), Data.VisionHull), Lifetime.Singleton).AsImplementedInterfaces();
            EntityFeatureBindings.RegisterSelection(builder, Bindings, SelectionType.Ship, SelectionType.Ship);
            if (Data.ShipClass is ShipClass.HeavyCruiser or ShipClass.Capital or ShipClass.BattleCruiser or ShipClass.Dreadnought)
            {
                builder.Register(resolver => new ShipMarqueeHull(View.transform,
                    resolver.Resolve<ShipSpawnClearance>().GetLocalHullBounds(_shipType)), Lifetime.Singleton)
                    .AsImplementedInterfaces();
            }
            EntityFeatureBindings.RegisterHighlight(builder, Bindings);
            EntityFeatureBindings.RegisterCombatModifiers(builder);
            EntityFeatureBindings.RegisterHealth(builder, Bindings, Data);
            EntityFeatureBindings.RegisterHullDamage(builder);
            EntityFeatureBindings.RegisterRadar(builder, Bindings);
            EntityFeatureBindings.RegisterWeapons(builder, Bindings, Data.ShipClass);
            foreach (var weapon in Bindings.Weapons)
            {
                weapon.ConfigureArtillery(Data.UsesArtilleryStandoff);
                weapon.ConfigureUnitFaction(_shipFaction);
            }
            EntityFeatureBindings.RegisterFog(builder, Bindings, !LocalPlayer.IsFriendly(Owner));
            builder.Register<ShipHullObstacle>(Lifetime.Singleton).AsImplementedInterfaces();
            if (Data.ShowBattleMarker)
                builder.Register<ShipBattleMarker>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipMoveModel>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            EntityFeatureBindings.RegisterComponent(builder, Bindings.ShipMove);
            if (Data.HangarBays.Count > 0)
            {
                builder.Register<HangarModel>(Lifetime.Singleton);
                EntityFeatureBindings.RegisterComponent(builder, Bindings.Hangar);
            }
            builder.Register<ShipSfxHandle>(Lifetime.Singleton);
            EntityFeatureBindings.RegisterComponent(builder, Bindings.AudioShip);
            if (LocalPlayer.IsLocal(Owner)) EntityFeatureBindings.RegisterComponent(builder, Bindings.AudioDialog);
            RegisterOrders(builder);
            builder.Register<ShipAbilityFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            if (Data.Abilities.Contains(ShipAbilityId.Interdict))
                EntityFeatureBindings.RegisterComponent(builder, Bindings.GravityWellEmitter);
            builder.Register<ShipAutoPowerFacade>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (Data.Abilities.Contains(ShipAbilityId.CompositeBeam))
                builder.Register<CompositeBeamFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipOrderFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipCombatObserverFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipDestroyFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipTooltipFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipSaveFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<UnitTypeFacade>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter("unitTypeId", UnitTypeId.Ship(_shipType));
        }

        private void RegisterOrders(IContainerBuilder builder)
        {
            builder.Register<ShipStateMachine>(Lifetime.Singleton);
            builder.Register<ShipThreatSense>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<AttackTargetState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces()
                .WithParameter<IArtilleryCapability>(resolver =>
                    Data.UsesArtilleryStandoff ? resolver.Resolve<IArtilleryCapability>() : null);
            builder.Register<IdleState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<NavigateState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<AttackMoveState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<GuardState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<HuntState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<AbilityApproachState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<UnitOrderModel>(Lifetime.Singleton);
            builder.Register<ShipOrderRunner>(Lifetime.Singleton).WithParameter(Owner);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.Register<ShipOrderTelemetrySource>(Lifetime.Singleton);
#endif
        }
    }
}
