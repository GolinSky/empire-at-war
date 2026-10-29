using System;
using UnityEngine;

namespace EmpireAtWar.Services.Settings
{
    [Serializable]
    public sealed class GraphicsSettingsData
    {
        public const int UNLIMITED_FRAME_RATE = 0;

        /// <summary>Quality level name; empty keeps the project's default level.</summary>
        [SerializeField] private string qualityPreset = string.Empty;

        [SerializeField] private int frameRateLimit = UNLIMITED_FRAME_RATE;

        [SerializeField] private bool vSync;
        [SerializeField] private bool nebulaEnabled = true;
        [SerializeField] private float renderResolution = 1f;
        [SerializeField] private GraphicsUpscaler upscaler;

        public bool NebulaEnabled { get => nebulaEnabled; set => nebulaEnabled = value; }
        public float RenderResolution { get => renderResolution; set => renderResolution = value; }
        public GraphicsUpscaler Upscaler { get => upscaler; set => upscaler = value; }

        public string QualityPreset
        {
            get => qualityPreset;
            set => qualityPreset = value;
        }

        public bool VSync
        {
            get => vSync;
            set => vSync = value;
        }

        /// <summary>Frames per second; <see cref="UNLIMITED_FRAME_RATE"/> removes the cap. Inactive while VSync is on.</summary>
        public int FrameRateLimit
        {
            get => frameRateLimit;
            set => frameRateLimit = value;
        }

        public bool Matches(GraphicsSettingsData other)
        {
            return qualityPreset == other.qualityPreset && vSync == other.vSync &&
                   frameRateLimit == other.frameRateLimit && nebulaEnabled == other.nebulaEnabled &&
                   renderResolution == other.renderResolution && upscaler == other.upscaler;
        }

        public void Sanitize()
        {
            qualityPreset ??= string.Empty;
            renderResolution = float.IsNaN(renderResolution) ? 1f : Mathf.Clamp(renderResolution, 0.5f, 1f);
            if (!Enum.IsDefined(typeof(GraphicsUpscaler), upscaler)) upscaler = GraphicsUpscaler.Bilinear;
            if (frameRateLimit < UNLIMITED_FRAME_RATE)
            {
                frameRateLimit = UNLIMITED_FRAME_RATE;
            }
        }
    }
}
