using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Players
{
    /// <summary>
    /// Team colors indexed by <see cref="PlayerSlot.ColorIndex"/>. The ship shader, the minimap and the
    /// skirmish setup screen all read this one asset, so a player looks the same everywhere.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(TeamColorPalette), menuName = "Data/TeamColorPalette")]
    public class TeamColorPalette : Data
    {
        [SerializeField] private TeamColorEntry[] entries =
        {
            new TeamColorEntry("Blue", new Color(0.2f, 0.55f, 1f)),
            new TeamColorEntry("Red", new Color(1f, 0.25f, 0.2f)),
            new TeamColorEntry("Green", new Color(0.3f, 0.9f, 0.4f)),
            new TeamColorEntry("Gold", new Color(1f, 0.75f, 0.2f)),
            new TeamColorEntry("Purple", new Color(0.7f, 0.35f, 1f)),
            new TeamColorEntry("Cyan", new Color(0.2f, 0.9f, 0.9f)),
            new TeamColorEntry("Pink", new Color(1f, 0.45f, 0.75f)),
            new TeamColorEntry("White", new Color(0.9f, 0.9f, 0.9f))
        };

        public int Count => entries.Length;

        public Color GetColor(int colorIndex)
        {
            return entries[colorIndex].Color;
        }

        public string GetName(int colorIndex)
        {
            return entries[colorIndex].Name;
        }

        /// <summary>Colors converted to linear space, the space the ship shader lights in.</summary>
        public Vector4[] CreateLinearColors()
        {
            Vector4[] linearColors = new Vector4[MatchRules.MAX_TEAM_COLORS];
            for (int i = 0; i < MatchRules.MAX_TEAM_COLORS; i++)
            {
                linearColors[i] = entries[i].Color.linear;
            }

            return linearColors;
        }
    }
}
