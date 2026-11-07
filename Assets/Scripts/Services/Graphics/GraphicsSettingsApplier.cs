using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Settings;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EmpireAtWar.Components.Environment;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Graphics
{
    /// <summary>Applies quality, frame pacing and user scaling to runtime pipeline copies.</summary>
    public sealed class GraphicsSettingsApplier : ISettingsApplier, IGraphicsOptions, IDisposable, ITickable
    {
        private const int NO_FRAME_RATE_CAP = -1;
#if UNITY_STANDALONE_WIN && ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA
        private const string DLSS_FRAME_SKIPPED = "[NVAPI] DLSS execute data pool exhausted - frame will be skipped";
        private volatile bool _dlssFrameSkipped;

        public GraphicsSettingsApplier()
        {
            Application.logMessageReceivedThreaded += OnLogMessage;
        }

        private void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (message == DLSS_FRAME_SKIPPED) _dlssFrameSkipped = true;
        }
#endif

        public void Tick()
        {
#if UNITY_STANDALONE_WIN && ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA
            if (!_dlssFrameSkipped) return;
            _dlssFrameSkipped = false;
            foreach (var entry in _runtimePipelines)
            {
                if (entry.Value.upscalingFilter != UpscalingFilterSelection.IUpscaler ||
                    entry.Value.upscalerName != DLSSIUpscaler.UPSCALER_NAME) continue;
                entry.Value.upscalingFilter = UpscalingFilterSelection.Linear;
                entry.Value.msaaSampleCount = _originalPipelines[entry.Key].msaaSampleCount;
            }
            GraphicsUpscalerSupport.DisableDlss();
#endif
        }

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
                    : graphics.FrameRateLimit == GraphicsSettingsData.MONITOR_FRAME_RATE
                        ? Mathf.Max(1, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value))
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
#if ENABLE_UPSCALER_FRAMEWORK
            // URP's temporal upscalers require single-sample rendering.
            pipeline.msaaSampleCount = graphics.Upscaler == GraphicsUpscaler.Dlss || graphics.Upscaler == GraphicsUpscaler.Fsr3
                ? 1 : _originalPipelines[level].msaaSampleCount;
            pipeline.supportsCameraOpaqueTexture = graphics.Upscaler == GraphicsUpscaler.Fsr3 ||
                _originalPipelines[level].supportsCameraOpaqueTexture;
            // URP 17.4 exposes these settings as read-only; edit only the runtime clone.
            int opaqueDownsampling = graphics.Upscaler == GraphicsUpscaler.Fsr3
                ? (int)Downsampling.None : (int)_originalPipelines[level].opaqueDownsampling;
            JsonUtility.FromJsonOverwrite("{\"m_OpaqueDownsampling\":" + opaqueDownsampling + "}", pipeline);
            if (graphics.Upscaler == GraphicsUpscaler.Fsr3)
                JsonUtility.FromJsonOverwrite("{\"m_IUpscalerName\":\"" + Fsr3Upscaler.UPSCALER_NAME + "\"}", pipeline);
#if UNITY_STANDALONE_WIN && ENABLE_NVIDIA
            else if (graphics.Upscaler == GraphicsUpscaler.Dlss)
                JsonUtility.FromJsonOverwrite("{\"m_IUpscalerName\":\"" + DLSSIUpscaler.UPSCALER_NAME + "\"}", pipeline);
#endif
#endif
            pipeline.upscalingFilter = graphics.Upscaler switch
            {
                GraphicsUpscaler.Fsr1 => UpscalingFilterSelection.FSR,
                GraphicsUpscaler.Stp => UpscalingFilterSelection.STP,
#if ENABLE_UPSCALER_FRAMEWORK
                GraphicsUpscaler.Fsr3 => UpscalingFilterSelection.IUpscaler,
#endif
#if UNITY_STANDALONE_WIN && ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA
                GraphicsUpscaler.Dlss => UpscalingFilterSelection.IUpscaler,
#endif
                _ => UpscalingFilterSelection.Linear,
            };
            pipeline.fsrOverrideSharpness = true;
            pipeline.fsrSharpness = 0.5f;
            MatchNebula.NebulaEnabled = graphics.NebulaEnabled;
        }

        public void Dispose()
        {
#if UNITY_STANDALONE_WIN && ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA
            Application.logMessageReceivedThreaded -= OnLogMessage;
#endif
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
