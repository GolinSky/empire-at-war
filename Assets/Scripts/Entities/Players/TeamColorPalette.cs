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
            new TeamColorEntry("Blue", new Color32(0x38, 0x71, 0xB4, 255)),
            new TeamColorEntry("Red", new Color32(0xBD, 0x40, 0x37, 255)),
            new TeamColorEntry("Green", new Color32(0x49, 0xA6, 0x59, 255)),
            new TeamColorEntry("Gold", new Color32(0xBE, 0x94, 0x38, 255)),
            new TeamColorEntry("Purple", new Color32(0x8D, 0x56, 0xBF, 255)),
            new TeamColorEntry("Cyan", new Color32(0x37, 0xA8, 0xA8, 255)),
            new TeamColorEntry("Pink", new Color32(0xC0, 0x61, 0x92, 255)),
            new TeamColorEntry("White", new Color32(0xB6, 0xB6, 0xB6, 255)),
            new TeamColorEntry("Indigo Blue", new Color32(0x4D, 0x68, 0xB7, 255)),
            new TeamColorEntry("Brick Red", new Color32(0xB7, 0x4C, 0x2B, 255)),
            new TeamColorEntry("Leaf Green", new Color32(0x69, 0xA3, 0x43, 255)),
            new TeamColorEntry("Ochre", new Color32(0xC3, 0x8B, 0x46, 255)),
            new TeamColorEntry("Blue Violet", new Color32(0x75, 0x5D, 0xC6, 255)),
            new TeamColorEntry("Jade Teal", new Color32(0x39, 0xAA, 0x91, 255)),
            new TeamColorEntry("Rose Pink", new Color32(0xBD, 0x64, 0x80, 255)),
            new TeamColorEntry("Warm Silver", new Color32(0xC0, 0xB5, 0xA5, 255))
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
