using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    /// <summary>Reads the same palette entry the ship shader uses, so UI and ships always agree.</summary>
    public sealed class PlayerColors : IPlayerColors
    {
        private readonly IPlayerRoster _roster;

        private readonly TeamColorPalette _palette;

        private static readonly Color NEUTRAL_COLOR = new Color(0.7f, 0.7f, 0.7f);

        public PlayerColors(IPlayerRoster roster, TeamColorPalette palette)
        {
            _roster = roster;
            _palette = palette;
        }

        public Color GetColor(PlayerId owner)
        {
            return owner.IsNone ? NEUTRAL_COLOR : _palette.GetColor(_roster.Get(owner).ColorIndex);
        }
    }
}
