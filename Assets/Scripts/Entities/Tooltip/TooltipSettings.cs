using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Tooltip;
using UnityEngine;

namespace EmpireAtWar.Entities.Tooltip
{
    [CreateAssetMenu(fileName = nameof(TooltipSettings), menuName = "Data/Tooltip/Settings")]
    public sealed class TooltipSettings : Data
    {
        [SerializeField] private Vector2 cursorOffset = new Vector2(18f, 24f);

        [SerializeField] private float showDelay = 0.35f;
        [SerializeField] private float refreshInterval = 0.1f;
        [SerializeField] private float screenEdgePadding = 12f;

        public TooltipTiming Timing => new TooltipTiming(showDelay, refreshInterval);
        public Vector2 CursorOffset => cursorOffset;
        public float ScreenEdgePadding => screenEdgePadding;
    }
}
