using System;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityTargeting
    {
        event Action TargetingChanged;

        bool IsWaitingForTarget { get; }

        /// <summary>True when at least one pending caster would accept the target, ignoring range.</summary>
        bool IsValidTarget(IEntity target);

        void SubmitTarget(IEntity target);

        void CancelTargeting();
    }
}
