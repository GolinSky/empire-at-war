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
        private SettingsService _service;

        [SetUp]
        public void SetUp()
        {
            _repository = new FakeRepository();
            _applier = new RecordingApplier();
            _service = new SettingsService(_repository, new List<ISettingsApplier> { _applier });
        }

        [Test]
        public void Initialize_WithSavedFile_AppliesItWithoutSaving()
        {
            SettingsData stored = new SettingsData();
            stored.Camera.PanSpeedMultiplier = 2f;
            _repository.Stored = stored;

            _service.Initialize();

            Assert.That(_service.PanSpeedMultiplier, Is.EqualTo(2f));
            Assert.That(_service.IsDirty, Is.False);
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
            Assert.That(_repository.SaveCount, Is.Zero);
        }

        [Test]
        public void Initialize_FirstRun_SavesDefaults()
        {
            _repository.Status = SettingsLoadStatus.Missing;

            _service.Initialize();

            Assert.That(_repository.SaveCount, Is.EqualTo(1));
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
        }

        [Test]
        public void Initialize_UnreadableFile_RunsOnDefaultsWithoutOverwriting()
        {
            _repository.Status = SettingsLoadStatus.Unreadable;

            _service.Initialize();

            Assert.That(_repository.SaveCount, Is.Zero);
            Assert.That(_service.Saved.Matches(new SettingsData()), Is.True);
        }

        [Test]
        public void Apply_CommitsDraftAndClearsDirty()
        {
            _service.Initialize();
            _service.Draft.Audio.MasterVolume = 0.5f;
            Assert.That(_service.IsDirty, Is.True);

            Assert.That(_service.Apply(), Is.EqualTo(SettingsApplyResult.Committed));

            Assert.That(_service.IsDirty, Is.False);
            Assert.That(_service.Saved.Audio.MasterVolume, Is.EqualTo(0.5f));
            Assert.That(_repository.Stored.Audio.MasterVolume, Is.EqualTo(0.5f));
        }

        [Test]
        public void Apply_DisplayChange_WaitsForConfirmationBeforeSaving()
        {
            _service.Initialize();
            _service.Draft.Display.WindowMode = DisplayWindowMode.Windowed;

            Assert.That(_service.Apply(), Is.EqualTo(SettingsApplyResult.AwaitingDisplayConfirmation));
            Assert.That(_service.IsAwaitingDisplayConfirmation, Is.True);
            Assert.That(_repository.SaveCount, Is.Zero);
            Assert.That(_applier.Last.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));

            Assert.That(_service.KeepDisplay(), Is.EqualTo(SettingsApplyResult.Committed));
            Assert.That(_service.IsAwaitingDisplayConfirmation, Is.False);
            Assert.That(_service.Saved.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));
        }

        [Test]
        public void RevertDisplay_RestoresSavedDisplayAndKeepsOtherEdits()
        {
            _service.Initialize();
            _service.Draft.Display.WindowMode = DisplayWindowMode.Windowed;
            _service.Draft.Camera.InvertZoom = true;
            _service.Apply();

            _service.RevertDisplay();

            Assert.That(_service.IsAwaitingDisplayConfirmation, Is.False);
            Assert.That(_applier.Last.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Borderless));
            Assert.That(_service.Draft.Camera.InvertZoom, Is.True);
            Assert.That(_service.IsDirty, Is.True);
            Assert.That(_repository.SaveCount, Is.Zero);
        }

        [Test]
        public void Discard_RestoresDraftAndReappliesSaved()
        {
            _service.Initialize();
            _service.Draft.Audio.MusicVolume = 0.1f;

            _service.Discard();

            Assert.That(_service.IsDirty, Is.False);
            Assert.That(_service.Draft.Audio.MusicVolume, Is.EqualTo(1f));
            Assert.That(_applier.Last, Is.SameAs(_service.Saved));
        }

        [Test]
        public void ResetDraftToDefaults_ChangesDraftOnly()
        {
            SettingsData stored = new SettingsData();
            stored.Graphics.VSync = true;
            _repository.Stored = stored;
            _service.Initialize();

            _service.ResetDraftToDefaults();

            Assert.That(_service.Draft.Graphics.VSync, Is.False);
            Assert.That(_service.Saved.Graphics.VSync, Is.True);
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
        }

        [Test]
        public void Apply_WhenSaveFails_ReportsFailureAndStaysDirty()
        {
            _service.Initialize();
            _service.Draft.Camera.EdgeScrolling = false;
            _repository.FailSaves = true;
            LogAssert.Expect(LogType.Error, new Regex("Could not save settings"));

            Assert.That(_service.Apply(), Is.EqualTo(SettingsApplyResult.SaveFailed));

            Assert.That(_service.IsDirty, Is.True);
            Assert.That(_service.EdgeScrolling, Is.True);
        }

        [Test]
        public void CameraPreferences_ReadSavedValuesNotDraft()
        {
            _service.Initialize();
            _service.Draft.Camera.ZoomSpeedMultiplier = 2f;

            Assert.That(_service.ZoomSpeedMultiplier, Is.EqualTo(1f));

            _service.Apply();

            Assert.That(_service.ZoomSpeedMultiplier, Is.EqualTo(2f));
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
