using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponComponent : IComponent, IMonoComponent
    {
        /// <summary>Longest firing distance of any hardpoint.</summary>
        float AttackDistance { get; }

        /// <summary>Distance to hold against this target class so most of the effective damage can fire.</summary>
        float GetEngagementRange(ShipClass targetClass);

        void AddTarget(AttackData attackData, AttackType attackType);

        bool HasEnoughRange(float distance, float contactDistance, ShipClass targetClass);

        void ResetTarget();
    }
}
