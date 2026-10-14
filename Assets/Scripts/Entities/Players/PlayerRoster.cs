using System.Collections.Generic;

namespace EmpireAtWar.Models.Players
{
    /// <summary>All players of the running match and the team rules between them.</summary>
    public sealed class PlayerRoster : IPlayerRoster
    {
        private readonly Dictionary<PlayerId, PlayerSlot> _slots = new Dictionary<PlayerId, PlayerSlot>();

        public IReadOnlyList<PlayerSlot> Players { get; private set; }

        public PlayerRoster(IReadOnlyList<PlayerSlot> players)
        {
            Replace(players);
        }

        public void Replace(IReadOnlyList<PlayerSlot> players)
        {
            _slots.Clear();
            Players = players;
            foreach (PlayerSlot slot in players)
            {
                _slots.Add(slot.Id, slot);
            }
        }

        public PlayerSlot Get(PlayerId id)
        {
            return _slots[id];
        }

        public bool IsHostile(PlayerId first, PlayerId second)
        {
            return !first.IsNone && !second.IsNone && _slots[first].Team != _slots[second].Team;
        }

        public bool IsAllied(PlayerId first, PlayerId second)
        {
            return !first.IsNone && !second.IsNone && _slots[first].Team == _slots[second].Team;
        }
    }
}
