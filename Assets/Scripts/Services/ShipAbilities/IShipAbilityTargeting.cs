using System;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityTargeting
    {
        event Action TargetingChanged;
        bool IsWaitingForTarget { get; }
        void SubmitTarget(IEntity target);
        void CancelTargeting();
    }
}
