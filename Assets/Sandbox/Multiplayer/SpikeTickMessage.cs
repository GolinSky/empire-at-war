using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Host → all, unreliable. One MTU-sized chunk of a 20 Hz tick.
    public struct SpikeTickMessage : NetworkMessage
    {
        public uint Tick;
        public double Time;
        public byte ChunkIndex;
        public byte ChunkCount;
        public SpikeUnitState[] Units;
    }
}
