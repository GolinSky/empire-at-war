using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
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

        /// <summary>True while the guns reach the target at full engagement range, not just the pursuit stop distance.</summary>
        bool IsWithinReach(float distance, float contactDistance, ShipClass targetClass);

        /// <summary>
        /// False while a live main battery that bears on the target with the bow turned at it is still short of every
        /// live target hardpoint. Mains that cannot bear bow-on, or would stay short even at
        /// <paramref name="contactDistance"/> (hull to hull), are ignored; if none bears, any main in reach suffices.
        /// True for a loadout without main batteries that can fire on it. Ranges scale by <paramref name="reachFactor"/>.
        /// With <paramref name="isHolding"/>, the bearing mains in reach need only half of the bearing main weight.
        /// </summary>
        bool MainBatteryReaches(IEntity target, float contactDistance, float reachFactor, bool isHolding);

        /// <summary>Offset to bring a target hardpoint into a live mount's range, or zero if already in reach.</summary>
        bool TryGetHardPointApproach(AttackData target, out Vector3 offset);

        bool CanUseSiegeApproach(IEntity target);

        void ResetTarget();
    }
}
