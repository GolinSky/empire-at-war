using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>
    /// Host → all, reliable: the full member list and battle setup after any lobby change. The setup is the
    /// host screen's lobby snapshot as JSON; clients render it read-only except their own row.
    /// </summary>
    public struct LobbyStateMessage : NetworkMessage
    {
        public LobbyMember[] Members;
        public int YourConnectionId;
        public LobbyMode Mode;
        public string SetupJson;
    }
}
