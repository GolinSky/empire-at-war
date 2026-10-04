using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.Settings;
using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Maps settings-screen row edits onto the settings draft and renders the draft into the model.</summary>
    public sealed class SettingsDraftEditor
    {
        private const string UNLIMITED_FRAME_RATE_LABEL = "Unlimited";

        private readonly ISettingsService _settingsService;
        private readonly IGraphicsOptions _graphicsOptions;
        private readonly IDisplayOptions _displayOptions;
        private readonly IAudioSettingsPreview _audioPreview;
        private IReadOnlyList<Vector2Int> _resolutions = new Vector2Int[0];

        private static readonly DisplayWindowMode[] WINDOW_MODES =
        {
            DisplayWindowMode.Windowed,
            DisplayWindowMode.Borderless,
            DisplayWindowMode.ExclusiveFullscreen,
        };
        private static readonly string[] WINDOW_MODE_LABELS = { "Windowed", "Borderless", "Fullscreen" };
        private static readonly int[] FRAME_RATE_LIMITS =
        {
            GraphicsSettingsData.UNLIMITED_FRAME_RATE, 30, 60, 90, 120, 144, 165, 240,
        };
        private readonly SettingsModel _model;
        private static readonly string[] FRAME_RATE_LIMIT_LABELS = FormatFrameRateLimits();
        private string[] _resolutionLabels = new string[0];

        private SettingsData Draft => _settingsService.Draft;

        public SettingsDraftEditor(
            ISettingsService settingsService,
            IGraphicsOptions graphicsOptions,
            IDisplayOptions displayOptions,
            IAudioSettingsPreview audioPreview,
            SettingsModel model)
        {
            _audioPreview = audioPreview;
            _settingsService = settingsService;
            _graphicsOptions = graphicsOptions;
            _displayOptions = displayOptions;
            _model = model;
        }

        /// <summary>Reads monitor modes once per screen opening; edits reuse the cached lists.</summary>
        public void Open()
        {
            _resolutions = _displayOptions.Resolutions;
            _resolutionLabels = FormatResolutions();
            Refresh();
        }

        public void Refresh()
        {
            GraphicsSettingsData graphics = Draft.Graphics;
            DisplaySettingsData display = Draft.Display;

            _model.SetGraphics(
                new SettingsChoice(
                    _graphicsOptions.QualityPresets,
                    _graphicsOptions.ResolveQualityLevel(graphics.QualityPreset),
                    true),
                new SettingsChoice(WINDOW_MODE_LABELS, Array.IndexOf(WINDOW_MODES, display.WindowMode), true),
                new SettingsChoice(
                    _resolutionLabels,
                    FindResolutionIndex(display),
                    display.WindowMode != DisplayWindowMode.Borderless),
                new SettingsChoice(
                    FRAME_RATE_LIMIT_LABELS,
                    Math.Max(0, Array.IndexOf(FRAME_RATE_LIMITS, graphics.FrameRateLimit)),
                    !graphics.VSync),
                graphics.VSync);

            AudioSettingsData audio = Draft.Audio;
            _model.SetAudio(
                audio.MasterVolume,
                audio.MusicVolume,
                audio.VoiceVolume,
                audio.SfxVolume,
                audio.MuteWhenUnfocused);

            CameraSettingsData camera = Draft.Camera;
            _model.SetCamera(
                camera.PanSpeedMultiplier,
                camera.ZoomSpeedMultiplier,
                camera.EdgeScrolling,
                camera.InvertZoom);
            _model.SetDirty(_settingsService.IsDirty);
        }

        public void SelectQuality(int index)
        {
            Draft.Graphics.QualityPreset = _graphicsOptions.QualityPresets[index];
            Refresh();
        }

        public void SelectWindowMode(int index)
        {
            Draft.Display.WindowMode = WINDOW_MODES[index];
            Refresh();
        }

        public void SelectResolution(int index)
        {
            Vector2Int size = _resolutions[index];
            Draft.Display.SetResolution(size.x, size.y);
            Refresh();
        }

        public void SelectFrameRateLimit(int index)
        {
            Draft.Graphics.FrameRateLimit = FRAME_RATE_LIMITS[index];
            Refresh();
        }

        public void SetVSync(bool isOn)
        {
            Draft.Graphics.VSync = isOn;
            Refresh();
        }

        public void SetMasterVolume(float volume)
        {
            Draft.Audio.MasterVolume = volume;
            PreviewAudio();
        }

        public void SetMusicVolume(float volume)
        {
            Draft.Audio.MusicVolume = volume;
            PreviewAudio();
        }

        public void SetVoiceVolume(float volume)
        {
            Draft.Audio.VoiceVolume = volume;
            PreviewAudio();
        }

        public void SetSfxVolume(float volume)
        {
            Draft.Audio.SfxVolume = volume;
            PreviewAudio();
        }

        public void SetMuteWhenUnfocused(bool isOn)
        {
            Draft.Audio.MuteWhenUnfocused = isOn;
            PreviewAudio();
        }

        /// <summary>Volumes are heard while editing; Discard re-applies the saved volumes.</summary>
        public void PreviewAudio()
        {
            _audioPreview.Preview(Draft.Audio);
            Refresh();
        }

        public void SetPanSpeed(float multiplier)
        {
            Draft.Camera.PanSpeedMultiplier = multiplier;
            Refresh();
        }

        public void SetZoomSpeed(float multiplier)
        {
            Draft.Camera.ZoomSpeedMultiplier = multiplier;
            Refresh();
        }

        public void SetEdgeScrolling(bool isOn)
        {
            Draft.Camera.EdgeScrolling = isOn;
            Refresh();
        }

        public void SetInvertZoom(bool isOn)
        {
            Draft.Camera.InvertZoom = isOn;
            Refresh();
        }

        private string[] FormatResolutions()
        {
            string[] labels = new string[_resolutions.Count];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = $"{_resolutions[i].x} x {_resolutions[i].y}";
            }

            return labels;
        }

        // A saved size missing on this monitor shows the desktop size, which is what the applier falls back to.
        private int FindResolutionIndex(DisplaySettingsData display)
        {
            Vector2Int desktop = _displayOptions.DesktopResolution;
            Vector2Int size = display.UsesDesktopResolution ? desktop : new Vector2Int(display.Width, display.Height);
            int index = IndexOf(size);
            return index >= 0 ? index : Math.Max(0, IndexOf(desktop));
        }

        private int IndexOf(Vector2Int size)
        {
            for (int i = 0; i < _resolutions.Count; i++)
            {
                if (_resolutions[i] == size)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string[] FormatFrameRateLimits()
        {
            string[] labels = new string[FRAME_RATE_LIMITS.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = FRAME_RATE_LIMITS[i] == GraphicsSettingsData.UNLIMITED_FRAME_RATE
                    ? UNLIMITED_FRAME_RATE_LABEL
                    : $"{FRAME_RATE_LIMITS[i]} FPS";
            }

            return labels;
        }
    }
}
