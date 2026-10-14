using EmpireAtWar.Entities.Game;
using System;
using System.Collections.Generic;

namespace EmpireAtWar.Models.Players
{
    /// <summary>Limits of a skirmish line-up: one station per map corner and exactly one human.</summary>
    public static class MatchRules
    {
        public static void Validate(BattleDefinition definition)
        {
            var players = definition.Players;
            if (definition.Outcome != OutcomeRule.Simulation)
                Validate(definition.Slots);
            else if (players.Count != 2 || players[0].Slot.Team == players[1].Slot.Team)
                throw new ArgumentException("Simulation requires two opposing sides.", nameof(definition));

            if (float.IsNaN(definition.StartingMoney) || float.IsInfinity(definition.StartingMoney) ||
                definition.StartingMoney < 0f ||
                definition.HasFeature(BattleFeatures.Economy) && definition.StartingMoney <= 0f)
                throw new ArgumentOutOfRangeException(nameof(definition), "An economy requires positive starting money.");

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.Slot.Id != new PlayerId(i) ||
                    (player.Role == PlayerRole.Human) == player.Slot.IsAi)
                    throw new ArgumentException("Player role must match its slot and controller.", nameof(definition));
                if (definition.Outcome == OutcomeRule.Simulation &&
                    player.Role != PlayerRole.SimulationSide)
                    throw new ArgumentException("Simulation requires SimulationSide roles.", nameof(definition));
                if (player.Role == PlayerRole.AiAlly &&
                    player.Slot.Team != FindHuman(definition.Slots).Team)
                    throw new ArgumentException("An AI ally must belong to the human's team.", nameof(definition));
            }
        }

        public const int MIN_PLAYERS = 2;
        public const int MAX_PLAYERS = 4;
        /// <summary>Size of the team color palette; must match MAX_TEAM_COLORS in ShipLitInput.hlsl.</summary>
        public const int MAX_TEAM_COLORS = 8;

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
            HashSet<int> colors = new HashSet<int>();
            for (int i = 0; i < players.Count; i++)
            {
                PlayerSlot slot = players[i];
                // Slot ids index per-player arrays and corner lists, so they must be 0..Count-1 in order.
                if (slot.Id != new PlayerId(i))
                {
                    throw new ArgumentException($"Player slot {i} has id {slot.Id}.", nameof(players));
                }

                if (slot.Controller == PlayerController.Human)
                {
                    humanCount++;
                }

                teams.Add(slot.Team);

                if (slot.ColorIndex < 0 || slot.ColorIndex >= MAX_TEAM_COLORS || !colors.Add(slot.ColorIndex))
                {
                    throw new ArgumentException($"Player slot {i} has an invalid or repeated color {slot.ColorIndex}.", nameof(players));
                }
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
