using Steamworks;

namespace EmpireAtWar.Services.Networking.Online
{
    public readonly struct SteamFriend
    {
        public readonly CSteamID Id;
        public readonly string Name;

        public SteamFriend(CSteamID id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
