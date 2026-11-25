using System;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityTargeting
    {
        event Action TargetingChanged;

        /// <summary>The local player's pick was accepted for the pending ability (local UI only).</summary>
        event Action<ShipAbilityId, IEntity> TargetAccepted;

        bool IsWaitingForTarget { get; }

        ShipAbilityId PendingAbilityId { get; }

        /// <summary>True when a pending caster can cast now or approach the target to cast.</summary>
        bool IsValidTarget(IEntity target);

        void SubmitTarget(IEntity target);

        void CancelTargeting();
    }
}
