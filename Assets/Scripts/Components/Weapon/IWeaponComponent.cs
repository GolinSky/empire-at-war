using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponComponent : IComponent, IMonoComponent
    {
        /// <summary>Longest firing distance of any hardpoint.</summary>
        float AttackDistance { get; }

        /// <summary>What the loadout is built to fight: ship AI picks these enemies before the others.</summary>
        TargetPreference PreferredTarget { get; }

        /// <summary>Distance to hold against this target class so most of the effective damage can fire.</summary>
        float GetEngagementRange(ShipClass targetClass);

        void AddTarget(AttackData attackData, AttackType attackType);

        /// <summary>True when a target from this health source is already tracked; lets callers skip building AttackData.</summary>
        bool HasTarget(IHealthFacade healthFacade);

        bool HasEnoughRange(float distance, float contactDistance, ShipClass targetClass);

        /// <summary>Offset to bring a target hardpoint into a live mount's range, or zero if already in reach.</summary>
        bool TryGetHardPointApproach(AttackData target, out Vector3 offset);

        bool CanUseSiegeApproach(IEntity target);

        /// <summary>True when a live ship-capable mount reaches <paramref name="position"/> in the plane.</summary>
        bool CanEngageMainWeapon(ShipClass targetClass, Vector3 position);

        void ResetTarget();
    }
}
