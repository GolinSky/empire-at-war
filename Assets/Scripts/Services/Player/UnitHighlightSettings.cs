using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    [CreateAssetMenu(fileName = nameof(UnitHighlightSettings), menuName = "Data/UnitHighlightSettings")]
    public sealed class UnitHighlightSettings : Data
    {
        [SerializeField] private Color color = Color.white;
        [SerializeField, Min(0f)] private float hoverStrength = 0.35f;
        [SerializeField, Min(0f)] private float selectedStrength = 0.25f;

        public Color Color => color;
        public float HoverStrength => hoverStrength;
        public float SelectedStrength => selectedStrength;
    }
}
