using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
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
        public async Task Initialize_WithSavedFile_AppliesItWithoutSaving()
        {
            SettingsData stored = new SettingsData();
            stored.Camera.PanSpeedMultiplier = 2f;
            _repository.Stored = stored;

            _settingsService.Initialize();
            await _settingsService.Initialization;

            Assert.That(_settingsService.PanSpeedMultiplier, Is.EqualTo(2f));
            Assert.That(_settingsService.IsDirty, Is.False);
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
            Assert.That(_repository.SaveCount, Is.Zero);
        }

        [Test]
        public async Task Initialize_FirstRun_SavesDefaults()
        {
            _repository.Status = SettingsLoadStatus.Missing;

            _settingsService.Initialize();
            await _settingsService.Initialization;

            Assert.That(_repository.SaveCount, Is.EqualTo(1));
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Initialize_UnreadableFile_RunsOnDefaultsWithoutOverwriting()
        {
            _repository.Status = SettingsLoadStatus.Unreadable;

            _settingsService.Initialize();
            await _settingsService.Initialization;

            Assert.That(_repository.SaveCount, Is.Zero);
            Assert.That(_settingsService.Saved.Matches(new SettingsData()), Is.True);
        }

        [Test]
        public async Task Apply_CommitsDraftAndClearsDirty()
        {
            _settingsService.Initialize();
            await _settingsService.Initialization;
            _settingsService.Draft.Audio.MasterVolume = 0.5f;
            Assert.That(_settingsService.IsDirty, Is.True);

            Assert.That(await _settingsService.ApplyAsync(), Is.EqualTo(SettingsApplyResult.Committed));

            Assert.That(_settingsService.IsDirty, Is.False);
            Assert.That(_settingsService.Saved.Audio.MasterVolume, Is.EqualTo(0.5f));
            Assert.That(_repository.Stored.Audio.MasterVolume, Is.EqualTo(0.5f));
        }

        [Test]
        public async Task Apply_DisplayChange_WaitsForConfirmationBeforeSaving()
        {
            _settingsService.Initialize();
            await _settingsService.Initialization;
            _settingsService.Draft.Display.WindowMode = DisplayWindowMode.Windowed;

            Assert.That(await _settingsService.ApplyAsync(), Is.EqualTo(SettingsApplyResult.AwaitingDisplayConfirmation));
            Assert.That(_settingsService.IsAwaitingDisplayConfirmation, Is.True);
            Assert.That(_repository.SaveCount, Is.Zero);
            Assert.That(_applier.Last.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));

            Assert.That(_settingsService.KeepDisplay(), Is.EqualTo(SettingsApplyResult.Committed));
            Assert.That(_settingsService.IsAwaitingDisplayConfirmation, Is.False);
            Assert.That(_settingsService.Saved.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));
        }

        [Test]
        public async Task RevertDisplay_RestoresSavedDisplayAndKeepsOtherEdits()
        {
            _settingsService.Initialize();
            await _settingsService.Initialization;
            _settingsService.Draft.Display.WindowMode = DisplayWindowMode.Windowed;
            _settingsService.Draft.Camera.InvertZoom = true;
            await _settingsService.ApplyAsync();

            await _settingsService.RevertDisplayAsync();

            Assert.That(_settingsService.IsAwaitingDisplayConfirmation, Is.False);
            Assert.That(_applier.Last.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Borderless));
            Assert.That(_settingsService.Draft.Camera.InvertZoom, Is.True);
            Assert.That(_settingsService.IsDirty, Is.True);
            Assert.That(_repository.SaveCount, Is.Zero);
        }

        [Test]
        public async Task Discard_RestoresDraftAndReappliesSaved()
        {
            _settingsService.Initialize();
            await _settingsService.Initialization;
            _settingsService.Draft.Audio.MusicVolume = 0.1f;

            await _settingsService.DiscardAsync();

            Assert.That(_settingsService.IsDirty, Is.False);
            Assert.That(_settingsService.Draft.Audio.MusicVolume, Is.EqualTo(1f));
            Assert.That(_applier.Last, Is.SameAs(_settingsService.Saved));
        }

        [Test]
        public async Task ResetDraftToDefaults_ChangesDraftOnly()
        {
            SettingsData stored = new SettingsData();
            stored.Graphics.VSync = true;
            _repository.Stored = stored;
            _settingsService.Initialize();
            await _settingsService.Initialization;

            _settingsService.ResetDraftToDefaults();

            Assert.That(_settingsService.Draft.Graphics.VSync, Is.False);
            Assert.That(_settingsService.Saved.Graphics.VSync, Is.True);
            Assert.That(_applier.Applied, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Apply_WhenSaveFails_ReportsFailureAndStaysDirty()
        {
            _settingsService.Initialize();
            await _settingsService.Initialization;
            _settingsService.Draft.Camera.EdgeScrolling = false;
            _repository.FailSaves = true;
            LogAssert.Expect(LogType.Error, new Regex("Could not save settings"));

            Assert.That(await _settingsService.ApplyAsync(), Is.EqualTo(SettingsApplyResult.SaveFailed));

            Assert.That(_settingsService.IsDirty, Is.True);
            Assert.That(_settingsService.EdgeScrolling, Is.True);
        }

        [Test]
        public async Task CameraPreferences_ReadSavedValuesNotDraft()
        {
            _settingsService.Initialize();
            await _settingsService.Initialization;
            _settingsService.Draft.Camera.ZoomSpeedMultiplier = 2f;

            Assert.That(_settingsService.ZoomSpeedMultiplier, Is.EqualTo(1f));

            await _settingsService.ApplyAsync();

            Assert.That(_settingsService.ZoomSpeedMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public async Task Gameplay_DraftAndDisplayRevertDoNotPublishUntilCommitted()
        {
            _settingsService.Initialize();
            await _settingsService.Initialization;
            int changes = 0;
            _settingsService.SavedChanged += () => changes++;
            _settingsService.Draft.Gameplay.AutoPowerAbilities = false;
            _settingsService.Draft.Display.WindowMode = DisplayWindowMode.Windowed;

            Assert.That(_settingsService.Saved.Gameplay.AutoPowerAbilities, Is.True);
            Assert.That(await _settingsService.ApplyAsync(), Is.EqualTo(SettingsApplyResult.AwaitingDisplayConfirmation));
            Assert.That(_settingsService.Saved.Gameplay.AutoPowerAbilities, Is.True);
            Assert.That(changes, Is.Zero);

            await _settingsService.RevertDisplayAsync();

            Assert.That(_settingsService.Draft.Gameplay.AutoPowerAbilities, Is.False);
            Assert.That(_settingsService.Saved.Gameplay.AutoPowerAbilities, Is.True);
            Assert.That(changes, Is.Zero);
            Assert.That(await _settingsService.ApplyAsync(), Is.EqualTo(SettingsApplyResult.Committed));
            Assert.That(_settingsService.Saved.Gameplay.AutoPowerAbilities, Is.False);
            Assert.That(_repository.Stored.Gameplay.AutoPowerAbilities, Is.False);
            Assert.That(changes, Is.EqualTo(1));

            _settingsService.Draft.Gameplay.AutoPowerAbilities = true;
            await _settingsService.DiscardAsync();
            Assert.That(_settingsService.Draft.Gameplay.AutoPowerAbilities, Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public async Task Gameplay_CombinedDisplayKeepPublishesOnce()
        {
            _settingsService.Initialize();
            await _settingsService.Initialization;
            int changes = 0;
            _settingsService.SavedChanged += () => changes++;
            _settingsService.Draft.Gameplay.AutoPowerAbilities = false;
            _settingsService.Draft.Display.WindowMode = DisplayWindowMode.Windowed;
            await _settingsService.ApplyAsync();

            _settingsService.KeepDisplay();

            Assert.That(_settingsService.Saved.Gameplay.AutoPowerAbilities, Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public async Task ApplyDisplay_WaitsForCompletionAndRevertRestoresActualDisplay()
        {
            DisplayApplier display = new DisplayApplier();
            display.Current.SetMonitor("Original", 0);
            display.Current.WindowMode = DisplayWindowMode.Windowed;
            display.Current.SetResolution(1280, 720);
            display.Current.SetRefreshRate(60000, 1001);
            DisplaySettingsData original = display.Current.Clone();
            SettingsService service = new SettingsService(_repository, new ISettingsApplier[] { display });
            service.Initialize();
            await service.Initialization;
            display.Current = original.Clone();
            TaskCompletionSource<bool> resize = new TaskCompletionSource<bool>();
            display.Completion = resize.Task;
            service.Draft.Display.SetMonitor("Other", 1);
            service.Draft.Display.SetResolution(2560, 1440);
            service.Draft.Display.SetRefreshRate(144, 1);

            Task<SettingsApplyResult> applying = service.ApplyAsync();
            Assert.That(applying.IsCompleted, Is.False);
            Assert.That(service.IsAwaitingDisplayConfirmation, Is.False);
            Assert.That(_repository.SaveCount, Is.Zero);
            resize.SetResult(true);
            Assert.That(await applying, Is.EqualTo(SettingsApplyResult.AwaitingDisplayConfirmation));

            await service.RevertDisplayAsync();

            Assert.That(display.Current.Matches(original), Is.True);
            Assert.That(service.Draft.Display.Matches(service.Saved.Display), Is.True);
            Assert.That(service.IsAwaitingDisplayConfirmation, Is.False);
        }

        [Test]
        public async Task PreviewMonitor_AppliesFullDisplayDraftBeforeConfirmation()
        {
            DisplayApplier display = new DisplayApplier();
            display.SupportedResolutions = new[] { new Vector2Int(1600, 900) };
            SettingsService service = new SettingsService(_repository, new ISettingsApplier[] { display });
            service.Initialize();
            await service.Initialization;
            service.Draft.Display.WindowMode = DisplayWindowMode.Windowed;
            service.Draft.Display.SetResolution(1600, 900);
            service.Draft.Display.SetRefreshRate(120, 1);

            await service.PreviewMonitorAsync(0);

            Assert.That(display.Current.Matches(service.Draft.Display), Is.True);
            Assert.That(display.Current.Width, Is.EqualTo(1600));
            Assert.That(display.Current.RefreshNumerator, Is.EqualTo(120));
            Assert.That(display.Current.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));
            Assert.That(service.IsAwaitingDisplayConfirmation, Is.True);
            Assert.That(_repository.SaveCount, Is.Zero);
        }

        [Test]
        public async Task PreviewMonitor_ResetsUnsupportedDraftSizeButRevertRestoresArbitraryBorderlessSize()
        {
            DisplayApplier display = new DisplayApplier();
            SettingsService service = new SettingsService(_repository, new ISettingsApplier[] { display });
            service.Initialize();
            await service.Initialization;
            display.Current.WindowMode = DisplayWindowMode.Borderless;
            display.Current.SetMonitor("Original", 1);
            display.Current.SetResolution(1234, 777);
            DisplaySettingsData original = display.Current.Clone();
            service.Draft.Display.SetResolution(1600, 900);

            await service.PreviewMonitorAsync(0);

            Assert.That(service.Draft.Display.UsesDesktopResolution, Is.True);
            Assert.That(display.Current.UsesDesktopResolution, Is.True);
            await service.RevertDisplayAsync();
            Assert.That(display.Current.Matches(original), Is.True);
            Assert.That(display.Current.Width, Is.EqualTo(1234));
            Assert.That(display.Current.Height, Is.EqualTo(777));
        }

        [Test]
        public async Task FailedDisplayApply_CanRestoreOriginalSnapshot()
        {
            DisplayApplier display = new DisplayApplier();
            SettingsService service = new SettingsService(_repository, new ISettingsApplier[] { display });
            service.Initialize();
            await service.Initialization;
            display.Current.SetMonitor("Original", 1);
            DisplaySettingsData original = display.Current.Clone();
            service.Draft.Display.SetMonitor("Unavailable", 2);
            display.Completion = Task.FromException(new System.InvalidOperationException("Move failed"));

            Assert.ThrowsAsync<System.InvalidOperationException>(async () => await service.ApplyAsync());
            Assert.That(service.IsAwaitingDisplayConfirmation, Is.False);
            display.Completion = Task.CompletedTask;
            await service.RevertDisplayAsync();

            Assert.That(display.Current.Matches(original), Is.True);
            Assert.That(service.Draft.Display.Matches(service.Saved.Display), Is.True);
        }

        private sealed class DisplayApplier : ISettingsApplier, IDisplayOptions
        {
            public DisplaySettingsData Current = new DisplaySettingsData();
            public Task Completion = Task.CompletedTask;
            public IReadOnlyList<Vector2Int> SupportedResolutions = new[] { new Vector2Int(1920, 1080) };
            public IReadOnlyList<Vector2Int> Resolutions => SupportedResolutions;
            public Vector2Int DesktopResolution => new Vector2Int(1920, 1080);
            public IReadOnlyList<DisplayWindowMode> WindowModes => new[] { DisplayWindowMode.Windowed, DisplayWindowMode.Borderless };
            public RefreshRate CurrentRefreshRate => new RefreshRate { numerator = 60, denominator = 1 };
            public IReadOnlyList<DisplayInfo> Monitors => new[] { default(DisplayInfo) };
            public DisplayInfo CurrentMonitor => default;
            public Task PendingApply => Completion;
            public IReadOnlyList<RefreshRate> RefreshRatesFor(Vector2Int size) => new[] { CurrentRefreshRate };
            public DisplaySettingsData CaptureCurrentDisplay() => Current.Clone();
            public void Apply(SettingsData settings) => Current = settings.Display.Clone();
            public async Task ApplyDisplayAsync(DisplaySettingsData display)
            {
                await Completion;
                Current = display.Clone();
            }
            public async Task MoveToMonitorAsync(DisplaySettingsData display)
            {
                await Completion;
                Current.SetMonitor(display.MonitorName, display.MonitorIndex);
            }
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
