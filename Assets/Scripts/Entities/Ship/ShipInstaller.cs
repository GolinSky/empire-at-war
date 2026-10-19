using System.Linq;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
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
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Ship
{
    public sealed class ShipInstaller : DynamicEntityInstaller<Ship, ShipData>
    {
        private readonly ShipType _shipType;
        private readonly ShipSpawnPose _pose;

        public ShipInstaller(Ship view, ShipData data, ILocalPlayer localPlayer, PlayerId owner,
            long entityId, Vector3 position, ShipType shipType, ShipSpawnPose pose)
            : base(view, data, localPlayer, owner, entityId, position)
        {
            _shipType = shipType;
            _pose = pose;
        }

        protected override void InstallFeatures(IContainerBuilder builder)
        {
            builder.RegisterInstance(_shipType);
            builder.RegisterInstance(_pose);
            builder.RegisterInstance(Data).AsSelf().As<IShipData, IShipMoveData, IRadarData, IHangarData>();
            EntityFeatureBindings.RegisterSelection(builder, Bindings, SelectionType.Ship, SelectionType.Ship);
            EntityFeatureBindings.RegisterCombatModifiers(builder);
            EntityFeatureBindings.RegisterHealth(builder, Bindings, Data);
            EntityFeatureBindings.RegisterRadar(builder, Bindings);
            EntityFeatureBindings.RegisterWeapons(builder, Bindings);
            EntityFeatureBindings.RegisterFog(builder, Bindings, !LocalPlayer.IsFriendly(Owner));
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
            if (Data.Abilities.Contains(ShipAbilityId.CompositeBeam))
                builder.Register<CompositeBeamFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipOrderFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipDestroyFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipTooltipFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShipSaveFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<UnitTypeFacade>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter("unitTypeId", UnitTypeId.Ship(_shipType));
        }

        private static void RegisterOrders(IContainerBuilder builder)
        {
            builder.Register<ShipStateMachine>(Lifetime.Singleton);
            builder.Register<AttackTargetState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<IdleState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<NavigateState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<AttackMoveState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<GuardState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<HuntState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<AbilityApproachState>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<UnitOrderModel>(Lifetime.Singleton);
            builder.Register<ShipOrderRunner>(Lifetime.Singleton);
        }
    }
}
