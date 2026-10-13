using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.EntityFacades.Combat;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Entities.Squadrons
{
    public sealed class SquadronInstaller : DynamicEntityInstaller<Squadron, SquadronData>
    {
        private readonly SquadronType _squadronType;
        private readonly Quaternion _rotation;

        public SquadronInstaller(Squadron view, SquadronData data, ILocalPlayer localPlayer, PlayerId owner,
            long entityId, Vector3 position, SquadronType squadronType, Quaternion rotation)
            : base(view, data, localPlayer, owner, entityId, position)
        {
            _squadronType = squadronType;
            _rotation = rotation;
        }

        protected override void InstallFeatures(IContainerBuilder builder)
        {
            builder.RegisterInstance(Data).AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(_squadronType);
            builder.RegisterInstance(_rotation);
            builder.Register<ShipSfxHandle>(Lifetime.Singleton);
            EntityFeatureBindings.RegisterSelection(builder, Bindings, SelectionType.Ship, SelectionType.Ship);
            EntityFeatureBindings.RegisterRadar(builder, Bindings);
            EntityFeatureBindings.RegisterWeapons(builder, Bindings);
            EntityFeatureBindings.RegisterFog(builder, Bindings, !LocalPlayer.IsFriendly(Owner));
            builder.Register<CombatModifiers>(Lifetime.Singleton);
            builder.Register<CombatModifiersFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SquadronHealthModel>(Lifetime.Singleton);
            if (Data.SeekerWarheadRange > 0f)
                builder.Register<SeekerWarheadCountermeasure>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<SquadronFlightModel>(Lifetime.Singleton);
            builder.Register<SFoilsModel>(Lifetime.Singleton);
            if (System.Array.IndexOf(Data.Abilities, ShipAbilityId.LockSFoils) >= 0)
            {
                EntityFeatureBindings.RegisterComponent(builder, Bindings.SFoils);
                builder.Register<SFoilsFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            }
            builder.Register<UnitOrderModel>(Lifetime.Singleton);
            builder.Register<SquadronPilot>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<SquadronTargetSelector>(Lifetime.Singleton);
            EntityFeatureBindings.RegisterComponent(builder, Bindings.SquadronHealth);
            EntityFeatureBindings.RegisterComponent(builder, Bindings.SquadronFlight);
            EntityFeatureBindings.RegisterComponent(builder, Bindings.SquadronIcon);
            if (LocalPlayer.IsLocal(Owner)) EntityFeatureBindings.RegisterComponent(builder, Bindings.AudioDialog);
            builder.Register<SquadronOrderFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SquadronAbilityFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HealthFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SquadronTooltipFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<UnitTypeFacade>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces()
                .WithParameter("unitTypeId", UnitTypeId.Squadron(_squadronType));
        }
    }
}
