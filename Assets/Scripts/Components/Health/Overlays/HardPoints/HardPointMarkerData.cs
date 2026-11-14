using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    public readonly struct HardPointMarkerData
    {
        private const float HOVER_SCALE = 1.12f;
        private const float TARGETED_SCALE = 1.25f;

        public Vector2 ScreenPosition { get; }
        public Sprite Icon { get; }
        public float HealthPercentage { get; }
        public bool IsHovered { get; }
        public bool IsTargeted { get; }
        public bool IsDestroyed { get; }
        /// <summary>Base marker scale before hover or targeting emphasis.</summary>
        public float Scale { get; }
        public float DisplayScale => Scale * GetEmphasisScale(IsHovered, IsTargeted);

        internal static float GetEmphasisScale(bool isHovered, bool isTargeted) =>
            isTargeted ? TARGETED_SCALE : isHovered ? HOVER_SCALE : 1f;

        public HardPointMarkerData(Sprite icon, Vector2 screenPosition, float healthPercentage,
            bool isHovered, bool isTargeted, bool isDestroyed, float scale)
        {
            ScreenPosition = screenPosition;
            Scale = scale;
            Icon = icon;
            HealthPercentage = healthPercentage;
            IsHovered = isHovered;
            IsTargeted = isTargeted;
            IsDestroyed = isDestroyed;
        }
    }
}
