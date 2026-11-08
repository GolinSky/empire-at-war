using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>
    /// Host → all, reliable: binds an entity id to its net index with its first keyframe.
    /// <see cref="Payload"/> is the unit's or structure's save JSON; clients spawn from it like a loaded save.
    /// </summary>
    public struct EntitySpawnMessage : NetworkMessage
    {
        public ushort Index;
        public long EntityId;
        public int OwnerIndex;
        public uint Tick;
        public double Time;
        public EntityKeyframe Keyframe;
        public EntitySpawnKind Kind;
        public string Payload;
    }
}
