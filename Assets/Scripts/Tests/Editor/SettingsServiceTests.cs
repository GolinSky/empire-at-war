using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using EmpireAtWar.Services.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SettingsServiceTests
    {
        private FakeRepository _repository;
        private RecordingApplier _applier;
        private SettingsService _settingsService;

        [SetUp]
        public void SetUp()
        {
            _repository = new FakeRepository();
            _applier = new RecordingApplier();
            _settingsService = new SettingsService(_repository, new List<ISettingsApplier> { _applier });
        }

        [Test]
        public void Initialize_WithSavedFile_AppliesItWithoutSaving()
        {
            SettingsData stored = new SettingsData();
            stored.Camera.PanSpeedMultiplier = 2f;
            _repository.Stored = stored;

            _settingsService.Initialize();

            Assert.That(_settingsService.PanSpeedMultiplier, Is.EqualTo(2f));
            Assert.That(_settingsService.IsDirty, Is.False);
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
            Assert.That(_repository.SaveCount, Is.Zero);
        }

        [Test]
        public void Initialize_FirstRun_SavesDefaults()
        {
            _repository.Status = SettingsLoadStatus.Missing;

            _settingsService.Initialize();

            Assert.That(_repository.SaveCount, Is.EqualTo(1));
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
        }

        [Test]
        public void Initialize_UnreadableFile_RunsOnDefaultsWithoutOverwriting()
        {
            _repository.Status = SettingsLoadStatus.Unreadable;

            _settingsService.Initialize();

            Assert.That(_repository.SaveCount, Is.Zero);
            Assert.That(_settingsService.Saved.Matches(new SettingsData()), Is.True);
        }

        [Test]
        public void Apply_CommitsDraftAndClearsDirty()
        {
            _settingsService.Initialize();
            _settingsService.Draft.Audio.MasterVolume = 0.5f;
            Assert.That(_settingsService.IsDirty, Is.True);

            Assert.That(_settingsService.Apply(), Is.EqualTo(SettingsApplyResult.Committed));

            Assert.That(_settingsService.IsDirty, Is.False);
            Assert.That(_settingsService.Saved.Audio.MasterVolume, Is.EqualTo(0.5f));
            Assert.That(_repository.Stored.Audio.MasterVolume, Is.EqualTo(0.5f));
        }

        [Test]
        public void Apply_DisplayChange_WaitsForConfirmationBeforeSaving()
        {
            _settingsService.Initialize();
            _settingsService.Draft.Display.WindowMode = DisplayWindowMode.Windowed;

            Assert.That(_settingsService.Apply(), Is.EqualTo(SettingsApplyResult.AwaitingDisplayConfirmation));
            Assert.That(_settingsService.IsAwaitingDisplayConfirmation, Is.True);
            Assert.That(_repository.SaveCount, Is.Zero);
            Assert.That(_applier.Last.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));

            Assert.That(_settingsService.KeepDisplay(), Is.EqualTo(SettingsApplyResult.Committed));
            Assert.That(_settingsService.IsAwaitingDisplayConfirmation, Is.False);
            Assert.That(_settingsService.Saved.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));
        }

        [Test]
        public void RevertDisplay_RestoresSavedDisplayAndKeepsOtherEdits()
        {
            _settingsService.Initialize();
            _settingsService.Draft.Display.WindowMode = DisplayWindowMode.Windowed;
            _settingsService.Draft.Camera.InvertZoom = true;
            _settingsService.Apply();

            _settingsService.RevertDisplay();

            Assert.That(_settingsService.IsAwaitingDisplayConfirmation, Is.False);
            Assert.That(_applier.Last.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Borderless));
            Assert.That(_settingsService.Draft.Camera.InvertZoom, Is.True);
            Assert.That(_settingsService.IsDirty, Is.True);
            Assert.That(_repository.SaveCount, Is.Zero);
        }

        [Test]
        public void Discard_RestoresDraftAndReappliesSaved()
        {
            _settingsService.Initialize();
            _settingsService.Draft.Audio.MusicVolume = 0.1f;

            _settingsService.Discard();

            Assert.That(_settingsService.IsDirty, Is.False);
            Assert.That(_settingsService.Draft.Audio.MusicVolume, Is.EqualTo(1f));
            Assert.That(_applier.Last, Is.SameAs(_settingsService.Saved));
        }

        [Test]
        public void ResetDraftToDefaults_ChangesDraftOnly()
        {
            SettingsData stored = new SettingsData();
            stored.Graphics.VSync = true;
            _repository.Stored = stored;
            _settingsService.Initialize();

            _settingsService.ResetDraftToDefaults();

            Assert.That(_settingsService.Draft.Graphics.VSync, Is.False);
            Assert.That(_settingsService.Saved.Graphics.VSync, Is.True);
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
        }

        [Test]
        public void Apply_WhenSaveFails_ReportsFailureAndStaysDirty()
        {
            _settingsService.Initialize();
            _settingsService.Draft.Camera.EdgeScrolling = false;
            _repository.FailSaves = true;
            LogAssert.Expect(LogType.Error, new Regex("Could not save settings"));

            Assert.That(_settingsService.Apply(), Is.EqualTo(SettingsApplyResult.SaveFailed));

            Assert.That(_settingsService.IsDirty, Is.True);
            Assert.That(_settingsService.EdgeScrolling, Is.True);
        }

        [Test]
        public void CameraPreferences_ReadSavedValuesNotDraft()
        {
            _settingsService.Initialize();
            _settingsService.Draft.Camera.ZoomSpeedMultiplier = 2f;

            Assert.That(_settingsService.ZoomSpeedMultiplier, Is.EqualTo(1f));

            _settingsService.Apply();

            Assert.That(_settingsService.ZoomSpeedMultiplier, Is.EqualTo(2f));
        }

        private sealed class FakeRepository : ISettingsRepository
        {
            public SettingsData Stored = new SettingsData();

            public SettingsLoadStatus Status = SettingsLoadStatus.Loaded;

            public int SaveCount;

            public bool FailSaves;

            public SettingsLoadStatus Load(out SettingsData data)
            {
                data = Status == SettingsLoadStatus.Loaded ? Stored.Clone() : null;
                return Status;
            }

            public void Save(SettingsData data)
            {
                if (FailSaves)
                {
                    throw new IOException("Disk full.");
                }

                SaveCount++;
                Stored = data.Clone();
            }
        }

        private sealed class RecordingApplier : ISettingsApplier
        {
            public readonly List<SettingsData> Applied = new List<SettingsData>();

            public SettingsData Last => Applied[Applied.Count - 1];

            public void Apply(SettingsData settings)
            {
                Applied.Add(settings);
            }
        }
    }
}
