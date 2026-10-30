using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.Player
{
    [CreateAssetMenu(fileName = nameof(UnitHighlightSettings), menuName = "Data/UnitHighlightSettings")]
    public sealed class UnitHighlightSettings : Data
    {
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(1f, 0.5f, 0f, 1f);
        [SerializeField] private Color attackColor = Color.red;
        [SerializeField, Min(0f)] private float outlineWidth = 2f;
        [SerializeField, Min(0f)] private float attackDuration = 2f;

        public Color HoverColor => hoverColor;
        public Color SelectedColor => selectedColor;
        public Color AttackColor => attackColor;
        public float OutlineWidth => outlineWidth;
        public float AttackDuration => attackDuration;
    }
}
