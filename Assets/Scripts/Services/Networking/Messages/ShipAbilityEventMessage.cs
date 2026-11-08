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
    }
}
