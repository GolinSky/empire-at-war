using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    public readonly struct HardPointMarkerData
    {
        public Vector2 ScreenPosition { get; }
        public Sprite Icon { get; }
        public float HealthPercentage { get; }
        public bool IsHovered { get; }
        public bool IsTargeted { get; }
        public bool IsDestroyed { get; }

        public HardPointMarkerData(Sprite icon, Vector2 screenPosition, float healthPercentage,
            bool isHovered, bool isTargeted, bool isDestroyed)
        {
            ScreenPosition = screenPosition;
            Icon = icon;
            HealthPercentage = healthPercentage;
            IsHovered = isHovered;
            IsTargeted = isTargeted;
            IsDestroyed = isDestroyed;
        }
    }
}
