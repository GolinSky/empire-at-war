using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → clients, reliable: these units were ordered to attack a target (hardpoint or -1 for any).</summary>
    public struct AttackTargetMessage : NetworkMessage
    {
        public long[] ReceiverIds;
        public long TargetId;
        public int HardPointId;
    }
}
