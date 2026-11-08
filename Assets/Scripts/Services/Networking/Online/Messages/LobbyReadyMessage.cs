using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>Client → host, reliable: ready tick toggled.</summary>
    public struct LobbyReadyMessage : NetworkMessage
    {
        public bool Ready;
    }
}
