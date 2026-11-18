using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Online;

namespace EmpireAtWar.Services.Player
{
    /// <summary>Readable player names for battle text: the online member's name, else "Faction AI" or "Player N".</summary>
    public sealed class PlayerDisplayNames
    {
        private readonly OnlineMatchService _online;
        private readonly IPlayerRoster _roster;

        public PlayerDisplayNames(OnlineMatchService online, IPlayerRoster roster)
        {
            _online = online;
            _roster = roster;
        }

        public string Get(PlayerId player)
        {
            foreach (var member in _online.Members)
                if (member.SlotIndex == player.Index) return member.Name;
            PlayerSlot slot = _roster.Get(player);
            return slot.IsAi ? $"{slot.Faction.GetDisplayName()} AI" : $"Player {player.Index + 1}";
        }
    }
}
