using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → all, unreliable: one chunk of a 20 Hz tick's keyframes.</summary>
    public struct EntityKeyframesMessage : NetworkMessage
    {
        public uint Tick;
        public double Time;
        public byte ChunkIndex;
        public byte ChunkCount;
        public EntityKeyframe[] Keyframes;
    }
}
