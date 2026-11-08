using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → all, reliable: the entity left the battle (destroyed or pooled away).</summary>
    public struct EntityDespawnMessage : NetworkMessage
    {
        public ushort Index;
        public uint Tick;
        public bool Destroyed;
    }
}
