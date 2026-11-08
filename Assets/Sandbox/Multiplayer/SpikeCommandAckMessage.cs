using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Host → issuer, reliable. Tick on which the command was applied (or rejected).
    public struct SpikeCommandAckMessage : NetworkMessage
    {
        public ushort Sequence;
        public uint AppliedTick;
        public bool Accepted;
    }
}
