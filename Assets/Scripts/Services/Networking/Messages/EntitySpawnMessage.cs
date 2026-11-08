using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>
    /// Host → all, reliable: binds an entity id to its net index with its first keyframe.
    /// What to spawn on the client (unit type, structure kind) is added with Phase 4's client spawn path.
    /// </summary>
    public struct EntitySpawnMessage : NetworkMessage
    {
        public ushort Index;
        public long EntityId;
        public int OwnerIndex;
        public uint Tick;
        public double Time;
        public EntityKeyframe Keyframe;
    }
}
