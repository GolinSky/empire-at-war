using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    public interface IHardPointOverlayView
    {
        /// <summary>Screen pixels per reference-resolution (1920x1080) pixel.</summary>
        float ScaleFactor { get; }

        void ShowMarker(int slot, HardPointMarkerData data);

        void HideMarkersFrom(int slot);

        /// <summary>Fades the marker out while moving it to <paramref name="screenPosition"/>; false once fully hidden.</summary>
        bool FadeOutMarker(int slot, Vector2 screenPosition);
    }
}
