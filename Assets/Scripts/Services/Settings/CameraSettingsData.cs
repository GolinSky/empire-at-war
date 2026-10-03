using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    /// <summary>Player multipliers applied on top of the authored <c>CameraData</c> values.</summary>
    [Serializable]
    public sealed class CameraSettingsData
    {
        public const float MIN_SPEED_MULTIPLIER = 0.25f;
        public const float MAX_SPEED_MULTIPLIER = 3f;

        [SerializeField] private float panSpeedMultiplier = 1f;
        [SerializeField] private float zoomSpeedMultiplier = 1f;

        [SerializeField] private bool edgeScrolling = true;
        [SerializeField] private bool invertZoom;

        public float PanSpeedMultiplier
        {
            get => panSpeedMultiplier;
            set => panSpeedMultiplier = ClampMultiplier(value);
        }

        public float ZoomSpeedMultiplier
        {
            get => zoomSpeedMultiplier;
            set => zoomSpeedMultiplier = ClampMultiplier(value);
        }

        public bool EdgeScrolling
        {
            get => edgeScrolling;
            set => edgeScrolling = value;
        }

        public bool InvertZoom
        {
            get => invertZoom;
            set => invertZoom = value;
        }

        public bool Matches(CameraSettingsData other)
        {
            return panSpeedMultiplier == other.panSpeedMultiplier &&
                   zoomSpeedMultiplier == other.zoomSpeedMultiplier &&
                   edgeScrolling == other.edgeScrolling && invertZoom == other.invertZoom;
        }

        public void Sanitize()
        {
            panSpeedMultiplier = ClampMultiplier(panSpeedMultiplier);
            zoomSpeedMultiplier = ClampMultiplier(zoomSpeedMultiplier);
        }

        private static float ClampMultiplier(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 1f;
            }

            return Mathf.Clamp(value, MIN_SPEED_MULTIPLIER, MAX_SPEED_MULTIPLIER);
        }
    }
}
