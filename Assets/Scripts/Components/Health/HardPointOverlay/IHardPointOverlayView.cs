using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    public interface IHardPointOverlayView
    {
        /// <summary>Screen pixels per reference-resolution (1920x1080) pixel.</summary>
        float ScaleFactor { get; }
        void ShowMarker(int slot, HardPointMarkerData data);
        void HideMarkersFrom(int slot);
        void ShowTooltip(Vector2 anchorScreenPosition, string title, string body);
        void HideTooltip();
    }
}
