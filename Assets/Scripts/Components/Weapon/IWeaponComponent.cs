using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Mvc;

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

        void ResetTarget();
    }
}
