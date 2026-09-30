using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Services.Graphics
{
    /// <summary>Applies the quality level and frame pacing. Each quality level owns its URP asset, so shared assets are never mutated.</summary>
    public sealed class GraphicsSettingsApplier : ISettingsApplier, IGraphicsOptions
    {
        private const int NO_FRAME_RATE_CAP = -1;

        private readonly int _defaultQualityLevel = QualitySettings.GetQualityLevel();
        // QualitySettings.names allocates a new array per read; the levels never change at runtime.
        private readonly string[] _qualityPresets = QualitySettings.names;

        public IReadOnlyList<string> QualityPresets => _qualityPresets;

        public int ResolveQualityLevel(string preset)
        {
            int level = Array.IndexOf(_qualityPresets, preset);
            return level >= 0 ? level : _defaultQualityLevel;
        }

        public void Apply(SettingsData settings)
        {
            GraphicsSettingsData graphics = settings.Graphics;
            int level = ResolveQualityLevel(graphics.QualityPreset);
            if (QualitySettings.GetQualityLevel() != level)
            {
                QualitySettings.SetQualityLevel(level, true);
            }

            // Set after the quality level, which carries its own vSyncCount.
            QualitySettings.vSyncCount = graphics.VSync ? 1 : 0;
            // Desktop ignores targetFrameRate while VSync is on, so VSync alone owns pacing then.
            Application.targetFrameRate =
                graphics.VSync || graphics.FrameRateLimit == GraphicsSettingsData.UNLIMITED_FRAME_RATE
                    ? NO_FRAME_RATE_CAP
                    : graphics.FrameRateLimit;
        }
    }
}
