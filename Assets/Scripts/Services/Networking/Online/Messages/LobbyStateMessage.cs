using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>Host → all, reliable: the full member list after any lobby change.</summary>
    public struct LobbyStateMessage : NetworkMessage
    {
        public LobbyMember[] Members;
        public int YourConnectionId;
    }
}
