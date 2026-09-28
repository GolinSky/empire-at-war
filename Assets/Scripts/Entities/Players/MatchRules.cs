using System;
using System.Collections.Generic;

namespace EmpireAtWar.Models.Players
{
    /// <summary>Limits of a skirmish line-up: one station per map corner and exactly one human.</summary>
    public static class MatchRules
    {
        public const int MIN_PLAYERS = 2;
        public const int MAX_PLAYERS = 4;

        /// <summary>The one human of a line-up; throws when there is none or more than one.</summary>
        public static PlayerSlot FindHuman(IReadOnlyList<PlayerSlot> players)
        {
            PlayerSlot human = null;
            foreach (PlayerSlot slot in players)
            {
                if (slot.Controller != PlayerController.Human)
                {
                    continue;
                }

                if (human != null)
                {
                    throw new InvalidOperationException("A skirmish supports exactly one human player.");
                }

                human = slot;
            }

            return human ?? throw new InvalidOperationException("A skirmish needs one human player.");
        }

        public static void Validate(IReadOnlyList<PlayerSlot> players)
        {
            if (players.Count < MIN_PLAYERS || players.Count > MAX_PLAYERS)
            {
                throw new ArgumentException(
                    $"A skirmish needs {MIN_PLAYERS}-{MAX_PLAYERS} players, got {players.Count}.", nameof(players));
            }

            int humanCount = 0;
            HashSet<TeamId> teams = new HashSet<TeamId>();
            for (int i = 0; i < players.Count; i++)
            {
                PlayerSlot slot = players[i];
                // Slot ids index palettes and corner lists, so they must be 0..Count-1 in order.
                if (slot.Id != new PlayerId(i))
                {
                    throw new ArgumentException($"Player slot {i} has id {slot.Id}.", nameof(players));
                }

                if (slot.Controller == PlayerController.Human)
                {
                    humanCount++;
                }

                teams.Add(slot.Team);
            }

            if (humanCount != 1)
            {
                throw new ArgumentException("A skirmish supports exactly one human player.", nameof(players));
            }

            if (teams.Count < 2)
            {
                throw new ArgumentException("A skirmish needs at least two opposing teams.", nameof(players));
            }
        }
    }
}
