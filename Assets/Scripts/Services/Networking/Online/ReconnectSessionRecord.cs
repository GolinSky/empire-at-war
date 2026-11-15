namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>Recovery destination and host-issued identity, never a battle snapshot.</summary>
    public sealed class ReconnectSessionRecord
    {
        public ReconnectTransport Transport { get; set; }
        public string Host { get; set; }
        public ushort Port { get; set; }
        public ulong LobbyId { get; set; }
        public ulong SteamId { get; set; }
        public string MatchId { get; set; }
        public string Credential { get; set; }
        public int SlotIndex { get; set; }
    }
}
