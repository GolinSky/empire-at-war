using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>Client → host, reliable, first message after connecting.</summary>
    public struct LobbyHelloMessage : NetworkMessage
    {
        public string Name;
        public string Version;
        public ulong SteamId;
    }
}
