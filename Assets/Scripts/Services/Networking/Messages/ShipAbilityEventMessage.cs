using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → clients, reliable: an ability started (target 0 = none) or was canceled.</summary>
    public struct ShipAbilityEventMessage : NetworkMessage
    {
        public long CasterId;
        public ShipAbilityId Ability;
        public long TargetId;
        public bool Canceled;
        public bool RestoreState;
        public ShipAbilityState State;
        public float TimeLeft;

        public static ShipAbilityEventMessage Capture(ShipAbilitySlot slot) => new ShipAbilityEventMessage
        {
            CasterId = slot.Owner.Entity.Id,
            Ability = slot.Id,
            TargetId = slot.RunningAbility is ITargetedShipAbility targeted ? targeted.Target.Id : 0,
            RestoreState = true,
            State = slot.State,
            TimeLeft = slot.TimeLeft
        };
    }
}
