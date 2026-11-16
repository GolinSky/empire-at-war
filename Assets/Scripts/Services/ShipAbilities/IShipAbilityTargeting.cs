using System;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityTargeting
    {
        event Action TargetingChanged;

        bool IsWaitingForTarget { get; }

        /// <summary>True when a pending caster can cast now or approach the target to cast.</summary>
        bool IsValidTarget(IEntity target);

        void SubmitTarget(IEntity target);

        void CancelTargeting();
    }
}
