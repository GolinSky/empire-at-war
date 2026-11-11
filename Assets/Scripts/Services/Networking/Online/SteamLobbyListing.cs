using Steamworks;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>One open lobby of this game and version, from the Steam lobby list.</summary>
    public readonly struct SteamLobbyListing
    {
        public readonly CSteamID Id;
        public readonly string HostName;
        public readonly string Mode;
        public readonly int Members;
        public readonly int Capacity;

        public SteamLobbyListing(CSteamID id, string hostName, string mode, int members, int capacity)
        {
            Id = id;
            HostName = hostName;
            Mode = mode;
            Members = members;
            Capacity = capacity;
        }
    }
}
