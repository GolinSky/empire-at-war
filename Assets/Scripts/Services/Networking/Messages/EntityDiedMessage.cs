using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → all, reliable: the entity died; clients play its death now (it despawns later).</summary>
    public struct EntityDiedMessage : NetworkMessage
    {
        public ushort Index;
    }
}
