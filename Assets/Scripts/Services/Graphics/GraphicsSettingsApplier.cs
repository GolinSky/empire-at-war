using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Settings;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EmpireAtWar.Components.Environment;

namespace EmpireAtWar.Services.Graphics
{
    /// <summary>Applies quality, frame pacing and user scaling to runtime pipeline copies.</summary>
    public sealed class GraphicsSettingsApplier : ISettingsApplier, IGraphicsOptions, IDisposable
    {
        private const int NO_FRAME_RATE_CAP = -1;

        // QualitySettings.names allocates a new array per read; the levels never change at runtime.
        private readonly string[] _qualityPresets = QualitySettings.names;

        private readonly int _defaultQualityLevel = QualitySettings.GetQualityLevel();
        private readonly Dictionary<int, UniversalRenderPipelineAsset> _originalPipelines = new();
        private readonly Dictionary<int, UniversalRenderPipelineAsset> _runtimePipelines = new();

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

            if (!_runtimePipelines.TryGetValue(level, out UniversalRenderPipelineAsset pipeline))
            {
                var original = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
                _originalPipelines.Add(level, original);
                pipeline = UnityEngine.Object.Instantiate(original);
                pipeline.hideFlags = HideFlags.DontSave;
                _runtimePipelines.Add(level, pipeline);
                QualitySettings.renderPipeline = pipeline;
            }

            if (Array.IndexOf(GraphicsUpscalerSupport.Available, graphics.Upscaler) < 0)
                graphics.Upscaler = GraphicsUpscaler.Bilinear;
            pipeline.renderScale = graphics.RenderResolution;
            pipeline.upscalingFilter = graphics.Upscaler switch
            {
                GraphicsUpscaler.Fsr1 => UpscalingFilterSelection.FSR,
                GraphicsUpscaler.Stp => UpscalingFilterSelection.STP,
                _ => UpscalingFilterSelection.Linear,
            };
            pipeline.fsrOverrideSharpness = true;
            pipeline.fsrSharpness = 0.5f;
            MatchNebula.NebulaEnabled = graphics.NebulaEnabled;
        }

        public void Dispose()
        {
            int currentLevel = QualitySettings.GetQualityLevel();
            foreach (var entry in _runtimePipelines)
            {
                QualitySettings.SetQualityLevel(entry.Key, false);
                QualitySettings.renderPipeline = _originalPipelines[entry.Key];
                if (Application.isPlaying) UnityEngine.Object.Destroy(entry.Value);
                else UnityEngine.Object.DestroyImmediate(entry.Value);
            }
            QualitySettings.SetQualityLevel(currentLevel, false);
            _runtimePipelines.Clear();
            _originalPipelines.Clear();
        }
    }
}
