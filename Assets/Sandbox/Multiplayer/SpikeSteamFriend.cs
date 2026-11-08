using Steamworks;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    public readonly struct SpikeSteamFriend
    {
        public readonly CSteamID Id;
        public readonly string Name;

        public SpikeSteamFriend(CSteamID id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
