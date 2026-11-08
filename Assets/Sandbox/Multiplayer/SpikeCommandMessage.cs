using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Client → host, reliable. Moves every unit of the issuer's side toward the target.
    public struct SpikeCommandMessage : NetworkMessage
    {
        public ushort Sequence;
        public uint LastReceivedTick;
        public byte Side;
        public short TargetX;
        public short TargetZ;
    }
}
