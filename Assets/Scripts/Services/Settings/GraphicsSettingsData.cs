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
        [SerializeField] private bool vSync;
        [SerializeField] private int frameRateLimit = UNLIMITED_FRAME_RATE;

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
                   frameRateLimit == other.frameRateLimit;
        }

        public void Sanitize()
        {
            qualityPreset ??= string.Empty;
            if (frameRateLimit < UNLIMITED_FRAME_RATE)
            {
                frameRateLimit = UNLIMITED_FRAME_RATE;
            }
        }
    }
}
