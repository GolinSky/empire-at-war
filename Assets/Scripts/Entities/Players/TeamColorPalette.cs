using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Players
{
    /// <summary>
    /// Team colors indexed by <see cref="PlayerSlot.ColorIndex"/>. Uploaded to the "_TeamColors"
    /// global array of the EmpireAtWar/Ship Lit shader once per battle.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(TeamColorPalette), menuName = "Data/TeamColorPalette")]
    public class TeamColorPalette : Data
    {
        /// <summary>Must match MAX_TEAM_COLORS in ShipLitInput.hlsl.</summary>
        public const int MAX_COLORS = 8;

        [SerializeField] private Color[] colors =
        {
            new Color(0.2f, 0.55f, 1f),
            new Color(1f, 0.25f, 0.2f),
            new Color(0.3f, 0.9f, 0.4f),
            new Color(1f, 0.75f, 0.2f),
            new Color(0.7f, 0.35f, 1f),
            new Color(0.2f, 0.9f, 0.9f),
            new Color(1f, 0.45f, 0.75f),
            new Color(0.9f, 0.9f, 0.9f)
        };

        public Color GetColor(int colorIndex)
        {
            return colors[colorIndex];
        }

        /// <summary>Colors converted to linear space, the space the shader lights in.</summary>
        public Vector4[] CreateLinearColors()
        {
            Vector4[] linearColors = new Vector4[MAX_COLORS];
            for (int i = 0; i < MAX_COLORS; i++)
            {
                linearColors[i] = colors[i].linear;
            }

            return linearColors;
        }
    }
}
