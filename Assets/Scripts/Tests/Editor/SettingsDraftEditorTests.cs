using System.Collections.Generic;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.Settings;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SettingsDraftEditorTests
    {
        private SettingsService _settings;
        private FakeDisplayOptions _display;
        private RecordingAudioPreview _audioPreview;
        private SettingsModel _model;
        private SettingsDraftEditor _editor;

        private static readonly Vector2Int DESKTOP = new Vector2Int(2560, 1440);
        private static readonly Vector2Int FULL_HD = new Vector2Int(1920, 1080);

        [SetUp]
        public void SetUp()
        {
            _settings = new SettingsService(new InMemoryRepository(), new List<ISettingsApplier>());
            _settings.Initialize();
            _display = new FakeDisplayOptions();
            _audioPreview = new RecordingAudioPreview();
            _model = new SettingsModel();
            _editor = new SettingsDraftEditor(_settings, new FakeGraphicsOptions(), _display, _audioPreview, _model);
        }

        [Test]
        public void Open_RendersDraftIntoModel()
        {
            _editor.Open();

            Assert.That(_model.Quality.Options, Is.EqualTo(new[] { "Low", "High" }));
            Assert.That(_model.Quality.Index, Is.EqualTo(1));
            Assert.That(_model.Resolution.Index, Is.EqualTo(0), "Desktop size is selected for the default draft.");
            Assert.That(_model.Resolution.Interactable, Is.False, "Borderless always uses the desktop size.");
            Assert.That(_model.FrameRateLimit.Interactable, Is.True);
            Assert.That(_model.MasterVolume, Is.EqualTo(1f));
            Assert.That(_model.IsDirty, Is.False);
        }

        [Test]
        public void Open_ReadsMonitorModesOncePerOpening()
        {
            _editor.Open();
            _editor.SetPanSpeed(1.5f);
            _editor.SetMasterVolume(0.5f);
            _editor.SelectResolution(1);

            Assert.That(_display.ResolutionReads, Is.EqualTo(1));
        }

        [Test]
        public void SelectWindowedResolution_WritesSizeAndMarksDirty()
        {
            _editor.Open();
            _editor.SelectWindowMode(0);
            _editor.SelectResolution(1);

            Assert.That(_settings.Draft.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));
            Assert.That(_settings.Draft.Display.Width, Is.EqualTo(FULL_HD.x));
            Assert.That(_model.Resolution.Index, Is.EqualTo(1));
            Assert.That(_model.Resolution.Interactable, Is.True);
            Assert.That(_model.IsDirty, Is.True);
        }

        [Test]
        public void SetVSync_DisablesFrameRateLimit()
        {
            _editor.Open();
            _editor.SetVSync(true);

            Assert.That(_model.VSync, Is.True);
            Assert.That(_model.FrameRateLimit.Interactable, Is.False);
        }

        [Test]
        public void SetVolume_PreviewsDraftAudioImmediately()
        {
            _editor.Open();
            _editor.SetMusicVolume(0.25f);

            Assert.That(_audioPreview.Previewed, Is.SameAs(_settings.Draft.Audio));
            Assert.That(_audioPreview.Previewed.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(_model.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(_model.IsDirty, Is.True);
        }

        [Test]
        public void EditsDoNotTouchSavedSettings()
        {
            _editor.Open();
            _editor.SetInvertZoom(true);
            _editor.SelectQuality(0);

            Assert.That(_settings.InvertZoom, Is.False);
            Assert.That(_settings.Saved.Graphics.QualityPreset, Is.Empty);
        }

        private sealed class FakeGraphicsOptions : IGraphicsOptions
        {
            private static readonly string[] PRESETS = { "Low", "High" };

            public IReadOnlyList<string> QualityPresets => PRESETS;

            public int ResolveQualityLevel(string preset)
            {
                return preset == "Low" ? 0 : 1;
            }
        }

        private sealed class FakeDisplayOptions : IDisplayOptions
        {
            public int ResolutionReads;

            public IReadOnlyList<Vector2Int> Resolutions
            {
                get
                {
                    ResolutionReads++;
                    return new[] { DESKTOP, FULL_HD };
                }
            }

            public Vector2Int DesktopResolution => DESKTOP;
        }

        private sealed class RecordingAudioPreview : IAudioSettingsPreview
        {
            public AudioSettingsData Previewed;

            public void Preview(AudioSettingsData audio)
            {
                Previewed = audio;
            }
        }

        private sealed class InMemoryRepository : ISettingsRepository
        {
            public SettingsLoadStatus Load(out SettingsData data)
            {
                data = new SettingsData();
                return SettingsLoadStatus.Loaded;
            }

            public void Save(SettingsData data)
            {
            }
        }
    }
}
