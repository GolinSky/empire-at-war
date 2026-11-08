using Mirror;

namespace EmpireAtWar.Services.Networking.Online.Messages
{
    /// <summary>Client → host, reliable: the sender's own Skirmish row choices (faction, team, color index).</summary>
    public struct LobbySlotEditMessage : NetworkMessage
    {
        public int Faction;
        public int Team;
        public int Color;
    }
}
