using System;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.Settings;

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
        private readonly SettingsDisplayEditor _displayEditor;

        private static readonly int[] FRAME_RATE_LIMITS =
        {
            GraphicsSettingsData.UNLIMITED_FRAME_RATE, GraphicsSettingsData.MONITOR_FRAME_RATE, 30, 60, 90, 120, 144, 165, 240,
        };
        private readonly SettingsModel _model;

        private GraphicsUpscaler[] _upscalers = GraphicsUpscalerSupport.Available;
        private string[] _upscalerLabels = Array.ConvertAll(GraphicsUpscalerSupport.Available, GraphicsUpscalerSupport.Label);


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
            _displayEditor = new SettingsDisplayEditor(displayOptions, settingsService, model);
        }

        /// <summary>Reads monitor modes once per screen opening; edits reuse the cached lists.</summary>
        public void Open()
        {
            _displayEditor.Open();
            Refresh();
        }

        public void Refresh()
        {
            GraphicsSettingsData graphics = Draft.Graphics;
            _displayEditor.Refresh();

            _model.SetGraphics(
                new SettingsChoice(
                    _graphicsOptions.QualityPresets,
                    _graphicsOptions.ResolveQualityLevel(graphics.QualityPreset),
                    true),
                _displayEditor.WindowMode,
                _displayEditor.Resolution,
                new SettingsChoice(
                    FormatFrameRateLimits(),
                    Math.Max(0, Array.IndexOf(FRAME_RATE_LIMITS, graphics.FrameRateLimit)),
                    !graphics.VSync),
                graphics.VSync);
            _model.SetRendering(graphics.NebulaEnabled, graphics.RenderResolution,
                new SettingsChoice(_upscalerLabels, Math.Max(0, Array.IndexOf(_upscalers, graphics.Upscaler)), true));

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
            _model.SetGameplay(Draft.Gameplay.AutoPowerAbilities);
            _model.SetPrivacy(Draft.Privacy.ShareUsageData);
            _model.SetDirty(_settingsService.IsDirty);
        }

        public void SelectQuality(int index)
        {
            Draft.Graphics.QualityPreset = _graphicsOptions.QualityPresets[index];
            Refresh();
        }

        public void SelectWindowMode(int index)
        {
            _displayEditor.SelectWindowMode(index);
            Refresh();
        }

        public void SelectResolution(int index)
        {
            _displayEditor.SelectResolution(index);
            Refresh();
        }

        public void SelectRefreshRate(int index)
        {
            _displayEditor.SelectRefreshRate(index);
            Refresh();
        }

        public void SelectFrameRateLimit(int index)
        {
            Draft.Graphics.FrameRateLimit = FRAME_RATE_LIMITS[index];
            Refresh();
        }

        public void SetNebulaEnabled(bool isOn)
        {
            Draft.Graphics.NebulaEnabled = isOn;
            Refresh();
        }

        public void SetRenderResolution(float resolution)
        {
            Draft.Graphics.RenderResolution = resolution;
            Refresh();
        }

        public void SelectUpscaler(int index)
        {
            Draft.Graphics.Upscaler = _upscalers[index];
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

        public void SetAutoPowerAbilities(bool isOn)
        {
            Draft.Gameplay.AutoPowerAbilities = isOn;
            Refresh();
        }

        public void SetShareUsageData(bool isOn)
        {
            Draft.Privacy.Answer(isOn);
            Refresh();
        }

        private string[] FormatFrameRateLimits()
        {
            string[] labels = new string[FRAME_RATE_LIMITS.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = FRAME_RATE_LIMITS[i] == GraphicsSettingsData.UNLIMITED_FRAME_RATE
                    ? UNLIMITED_FRAME_RATE_LABEL
                    : FRAME_RATE_LIMITS[i] == GraphicsSettingsData.MONITOR_FRAME_RATE
                        ? $"Monitor ({SettingsDisplayEditor.FormatRate(_displayOptions.CurrentRefreshRate)})"
                        : $"{FRAME_RATE_LIMITS[i]} FPS";
            }

            return labels;
        }
    }
}
