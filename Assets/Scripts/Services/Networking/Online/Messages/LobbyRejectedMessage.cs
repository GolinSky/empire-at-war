using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>Host → one client, reliable, just before the host drops the connection.</summary>
    public struct LobbyRejectedMessage : NetworkMessage
    {
        public OnlineError Reason;
    }
}
