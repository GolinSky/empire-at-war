using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.CinematicCamera.Controller;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Repository;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Saves;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleSaveServiceTests
    {
        private GameObject _cameraObject;
        private FakeCamera _camera;
        private FakeCinematic _cinematic;
        private SkirmishStateModel _state;
        private string _directory;
        private JsonBattleSaveRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject(nameof(BattleSaveServiceTests));
            _cameraObject.transform.SetPositionAndRotation(new Vector3(-120f, 600f, 75f), Quaternion.Euler(60f, -35f, 0f));
            _camera = new FakeCamera(_cameraObject.transform, 540f);
            _cinematic = new FakeCinematic();
            _state = new SkirmishStateModel();
            _directory = Path.Combine(Path.GetTempPath(), "BattleSaveServiceTests_" + Guid.NewGuid().ToString("N"));
            _repository = new JsonBattleSaveRepository(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraObject);
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [TestCase(BattleState.Running)]
        [TestCase(BattleState.Paused)]
        public void Capture_RunningOrPausedBattle_RecordsHeaderSetupAndCamera(BattleState state)
        {
            BattleDefinition battle = new EmpireAtWar.Entities.MainMenu.Skirmish.SkirmishModel().CreateBattle();
            _state.SetState(state);
            _state.SetSpeed(GameSpeed.Fast);
            BattleSaveService service = CreateService(battle);

            BattleSave save = service.Capture(BattleSaveSlot.Quicksave);

            Assert.That(save.Header.Mode, Is.EqualTo(GameMode.Skirmish));
            Assert.That(save.Header.Slot, Is.SameAs(BattleSaveSlot.Quicksave));
            Assert.That(save.Header.GameSpeed, Is.EqualTo(GameSpeed.Fast));
            Assert.That(save.Header.BattleTime, Is.Zero);
            Assert.That(save.Header.SavedAtUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));
            Assert.That(save.Camera.Position, Is.EqualTo(_cameraObject.transform.position));
            Assert.That(save.Camera.Rotation, Is.EqualTo(_cameraObject.transform.rotation));
            Assert.That(save.Camera.ZoomTargetHeight, Is.EqualTo(540f));
            Assert.That(save.Setup.Rebuild().Map.Seed, Is.EqualTo(battle.Map.Seed));
        }

        [Test]
        public void Capture_DuringCinematic_RecordsTheGameplayPoseItReturnsTo()
        {
            _state.SetState(BattleState.Running);
            _cinematic.IsActive = true;
            _cinematic.Position = new Vector3(40f, 450f, -10f);
            _cinematic.Rotation = Quaternion.Euler(55f, 20f, 0f);

            BattleSave save = CreateService(SkirmishPreset.Default()).Capture(BattleSaveSlot.Autosave);

            Assert.That(save.Camera.Position, Is.EqualTo(_cinematic.Position));
            Assert.That(save.Camera.Rotation, Is.EqualTo(_cinematic.Rotation));
            Assert.That(save.Camera.ZoomTargetHeight, Is.EqualTo(450f));
        }

        [Test]
        public void Capture_ThroughRepository_LoadsBackSameCameraAndSetup()
        {
            BattleDefinition battle = new EmpireAtWar.Entities.MainMenu.Survival.SurvivalModel().CreateBattle();
            _state.SetState(BattleState.Running);
            _repository.Save(CreateService(battle).Capture(BattleSaveSlot.Named("Wave 3")));

            BattleSave loaded = _repository.Load(GameMode.Survival, BattleSaveSlot.Named("Wave 3"));

            Assert.That(loaded.Camera.Position, Is.EqualTo(_cameraObject.transform.position));
            Assert.That(Quaternion.Angle(loaded.Camera.Rotation, _cameraObject.transform.rotation), Is.LessThan(0.01f));
            Assert.That(loaded.Camera.ZoomTargetHeight, Is.EqualTo(540f));
            Assert.That(loaded.Setup.Rebuild().Survival.EnemyFaction, Is.EqualTo(battle.Survival.EnemyFaction));
        }

        [TestCase(BattleState.NotInitialized)]
        [TestCase(BattleState.Loading)]
        [TestCase(BattleState.Ended)]
        public void Capture_LoadingOrEndedBattle_IsRefused(BattleState state)
        {
            _state.SetState(state);
            BattleSaveService service = CreateService(SkirmishPreset.Default());

            Assert.That(service.CanSave, Is.False);
            Assert.Throws<InvalidOperationException>(() => service.Capture(BattleSaveSlot.Quicksave));
        }

        [Test]
        public void Capture_Simulation_IsRefused()
        {
            SimulationFleetEntry[] fleet = { new SimulationFleetEntry(UnitTypeId.Ship(ShipType.Venator), 1) };
            BattleDefinition battle = SimulationPreset.Create(new SimulationSetup(FactionType.Empire,
                FactionType.Rebellion, fleet, fleet, 1000f, 60f, false)).Build();
            _state.SetState(BattleState.Running);
            BattleSaveService service = CreateService(battle);

            Assert.That(service.CanSave, Is.False);
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => service.Capture(BattleSaveSlot.Quicksave));
            Assert.That(error.Message, Does.Contain("Simulation"));
        }

        [TestCase(BattleState.Loading)]
        [TestCase(BattleState.Paused)]
        [TestCase(BattleState.Ended)]
        public void Clock_StandsStillUnlessRunning(BattleState state)
        {
            _state.SetState(state);
            BattleClock clock = new BattleClock(_state);

            clock.Tick();

            Assert.That(clock.Elapsed, Is.Zero);
        }

        [Test]
        public void RestoreStep_ConsumesSaveAfterRestoringClockSpeedAndCamera()
        {
            var game = ScriptableObject.CreateInstance<GameData>();
            try
            {
                _state.SetState(BattleState.Running);
                _state.SetSpeed(GameSpeed.Fast);
                var clock = new BattleClock(_state);
                clock.Restore(723f);
                var units = new UnitSaveService(new EntityLocator(), new BattleUnitSpawner(), null);
                var service = new BattleSaveService(SkirmishPreset.Default(), _state, _camera,
                    _cinematic, clock, _repository, units, Array.Empty<IBattleSaveSource>());
                game.PendingBattleSave = BattleSave.FromJson(service.Capture(BattleSaveSlot.Quicksave).ToJson());
                clock.Restore(0f);
                _state.SetSpeed(GameSpeed.Normal);
                _cameraObject.transform.position = Vector3.zero;
                var restore = new BattleRestoreStep(game, new BattleRestorer(units, Array.Empty<IBattleSaveSource>()),
                    clock, _state, _camera);

                Assert.That(restore.TryRestore(), Is.True);
                Assert.That(clock.Elapsed, Is.EqualTo(723f));
                Assert.That(_state.Speed, Is.EqualTo(GameSpeed.Fast));
                Assert.That(_camera.CameraPosition, Is.EqualTo(new Vector3(-120f, 600f, 75f)));
                Assert.That(_camera.RestoredZoom, Is.EqualTo(540f));
                Assert.That(game.PendingBattleSave, Is.Null);
                Assert.That(restore.TryRestore(), Is.False);
            }
            finally { Object.DestroyImmediate(game); }
        }

        [Test]
        public void RestoreStep_WhenRestorerThrows_ClearsPendingSaveAndRecordsPendingLoadError()
        {
            var game = ScriptableObject.CreateInstance<GameData>();
            try
            {
                _state.SetState(BattleState.Running);
                var clock = new BattleClock(_state);
                var units = new UnitSaveService(new EntityLocator(), new BattleUnitSpawner(), null);
                var service = new BattleSaveService(SkirmishPreset.Default(), _state, _camera,
                    _cinematic, clock, _repository, units, Array.Empty<IBattleSaveSource>());
                game.PendingBattleSave = service.Capture(BattleSaveSlot.Quicksave);
                var restore = new BattleRestoreStep(game,
                    new BattleRestorer(units, new IBattleSaveSource[] { new ThrowingSaveSource() }),
                    clock, _state, _camera);

                Assert.Throws<InvalidOperationException>(() => restore.TryRestore());
                Assert.That(game.PendingBattleSave, Is.Null);
                Assert.That(game.PendingLoadError, Does.StartWith("Load failed:").And.Contain("Simulated restore failure"));
            }
            finally { Object.DestroyImmediate(game); }
        }

        [Test]
        public void Validator_RejectsMismatchedCountsMissingReferencesAndInvalidEnums()
        {
            using var assets = new CachedAssetService(new AddressableHandleLoader());
            var mapSettings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            Assert.That(mapSettings, Is.Not.Null);
            var validator = new BattleSaveValidator(assets, mapSettings);
            BattleDefinition definition = SkirmishPreset.Default();
            var header = BattleSaveHeader.Create(GameMode.Skirmish, BattleSaveSlot.Quicksave, DateTime.UtcNow, 10f, GameSpeed.Normal);
            var setup = BattleSetupSave.From(definition);
            var camera = CameraPoseSave.Create(Vector3.zero, Quaternion.identity, 500f);

            var wrongCaptureSites = BattleSave.Create(header, setup, camera,
                Array.Empty<UnitSave>(), Array.Empty<StructureSave>());
            wrongCaptureSites.CaptureSites = new[]
            {
                CaptureSiteSave.Create(PlayerId.None, CaptureSiteState.Neutral, PlayerId.None, 0f, SiteFacilityType.Mining, 0f, 0L)
            };
            Assert.Throws<InvalidDataException>(() => validator.Validate(wrongCaptureSites, definition));

            var missingFacilitySave = BattleSave.Create(header, setup, camera,
                Array.Empty<UnitSave>(), Array.Empty<StructureSave>());
            missingFacilitySave.CaptureSites = CreateDefaultCaptureSites(mapSettings, definition);
            missingFacilitySave.CaptureSites[0] = CaptureSiteSave.Create(new PlayerId(0),
                CaptureSiteState.Operational, PlayerId.None, 0f, SiteFacilityType.Mining, 0f, 999L);
            missingFacilitySave.ReinforcementZones = CreateDefaultReinforcementZones(mapSettings, definition);
            Assert.That(Assert.Throws<InvalidDataException>(() => validator.Validate(missingFacilitySave, definition)).Message,
                Does.Contain("facility 999 is missing"));

            var badPlayer = PlayerSave.Create(new PlayerId(0));
            badPlayer.Research = new[] { ResearchSave.Create(ResearchType.IncreasedProduction, 99) };
            var badPlayerSave = BattleSave.Create(header, setup, camera,
                Array.Empty<UnitSave>(), Array.Empty<StructureSave>());
            badPlayerSave.CaptureSites = CreateDefaultCaptureSites(mapSettings, definition);
            badPlayerSave.ReinforcementZones = CreateDefaultReinforcementZones(mapSettings, definition);
            badPlayerSave.Players = new[] { badPlayer };
            Assert.That(Assert.Throws<InvalidDataException>(() => validator.Validate(badPlayerSave, definition)).Message,
                Does.Contain("Saved research"));
            badPlayer.Research = new[] { ResearchSave.Create((ResearchType)int.MaxValue, 1) };
            Assert.That(Assert.Throws<InvalidDataException>(() => validator.Validate(badPlayerSave, definition)).Message,
                Does.Contain("Saved research"));
        }

        private static CaptureSiteSave[] CreateDefaultCaptureSites(MapGenerationSettings mapSettings, BattleDefinition definition)
        {
            MapSizeSettings size = mapSettings.GetSize(definition.Map.Size);
            int count = (definition.Slots.Count + ((size.MiningPointCount - 2) / 2) * 2) * PointOfInterestPlacer.SITES_PER_MINING_POINT
                + (size.BattleSiteCount / 2) * 2;
            var sites = new CaptureSiteSave[count];
            for (int i = 0; i < count; i++)
                sites[i] = CaptureSiteSave.Create(PlayerId.None, CaptureSiteState.Neutral, PlayerId.None, 0f, SiteFacilityType.Mining, 0f, 0L);
            return sites;
        }

        private static ReinforcementZoneSave[] CreateDefaultReinforcementZones(MapGenerationSettings mapSettings, BattleDefinition definition)
        {
            int count = mapSettings.GetSize(definition.Map.Size).CapturableZoneCount;
            var zones = new ReinforcementZoneSave[count];
            for (int i = 0; i < count; i++)
                zones[i] = ReinforcementZoneSave.Create(PlayerId.None, PlayerId.None, 0f, false);
            return zones;
        }

        private sealed class ThrowingSaveSource : IBattleSaveSource
        {
            public void Capture(BattleSave save) { }
            public void RestoreBeforeUnits(BattleSave save) =>
                throw new InvalidOperationException("Simulated restore failure.");
            public void RestoreAfterUnits(BattleRestoreContext context) { }
        }

        [UnityTest]
        public IEnumerator SaveActions_DisposedDuringWrite_DoesNotNotifyCompletion() =>
            DisposeDuringSave(false);

        [UnityTest]
        public IEnumerator SaveActions_DisposedDuringFailedWrite_DoesNotNotifyFailure() =>
            DisposeDuringSave(true);

        private IEnumerator DisposeDuringSave(bool fail)
        {
            _state.SetState(BattleState.Running);
            BattleDefinition battle = SkirmishPreset.Default();
            using var repository = new BlockingRepository(fail);
            var service = new BattleSaveService(battle, _state, _camera, _cinematic,
                new BattleClock(_state), repository,
                new UnitSaveService(new EntityLocator(), new BattleUnitSpawner(), null),
                Array.Empty<IBattleSaveSource>());
            using var actions = new BattleSaveActions(service, repository, null, battle);
            var statuses = new List<string>();
            actions.StatusChanged += statuses.Add;
            Task save = actions.SaveAsync(BattleSaveSlot.Quicksave);
            float deadline = Time.realtimeSinceStartup + 10f;
            try
            {
                while (!repository.Started.IsSet && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(repository.Started.IsSet, Is.True, "Save did not reach the worker thread.");
                Assert.That(service.IsSaving, Is.True);
                Assert.That(statuses.Count, Is.EqualTo(1));
                actions.Dispose();
            }
            finally { repository.Continue.Set(); }
            while (!save.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(save.IsCompleted, Is.True, "Save did not finish after releasing the repository.");
            save.GetAwaiter().GetResult();
            Assert.That(service.IsSaving, Is.False);
            Assert.That(statuses.Count, Is.EqualTo(1), "Disposed battle must not receive a save continuation.");
            Assert.That(actions.Status, Is.EqualTo(statuses[0]));
            Assert.That(actions.CanSave, Is.False);
            Task ignored = actions.SaveAsync(BattleSaveSlot.Quicksave);
            Assert.That(ignored.IsCompleted, Is.True);
            Assert.That(repository.WriteCount, Is.EqualTo(1));
        }

        private sealed class BlockingRepository : IBattleSaveRepository, IDisposable
        {
            private readonly bool _fail;
            public ManualResetEventSlim Started { get; } = new ManualResetEventSlim();
            public ManualResetEventSlim Continue { get; } = new ManualResetEventSlim();
            public int WriteCount { get; private set; }

            public BlockingRepository(bool fail) { _fail = fail; }

            public void Save(BattleSave save)
            {
                WriteCount++;
                Started.Set();
                if (!Continue.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                if (_fail) throw new IOException("Test write failure.");
            }

            public BattleSave Load(GameMode mode, BattleSaveSlot slot) => throw new NotSupportedException();
            public void Delete(GameMode mode, BattleSaveSlot slot) => throw new NotSupportedException();
            public IReadOnlyList<BattleSaveHeader> List(GameMode mode) => throw new NotSupportedException();
            public void Dispose()
            {
                Started.Dispose();
                Continue.Dispose();
            }
        }

        private BattleSaveService CreateService(BattleDefinition battle) =>
            new BattleSaveService(battle, _state, _camera, _cinematic, new BattleClock(_state), _repository,
                new UnitSaveService(new EntityLocator(), new BattleUnitSpawner(), null), Array.Empty<IBattleSaveSource>());

        [TestCase(BattleState.NotInitialized, false)]
        [TestCase(BattleState.Loading, false)]
        [TestCase(BattleState.Running, true)]
        [TestCase(BattleState.Paused, true)]
        [TestCase(BattleState.Ended, true)]
        public void QuickLoad_AllowsEndedBattlesButNotStartup(BattleState state, bool allowed)
        {
            var battle = SkirmishPreset.Default();
            _state.SetState(BattleState.Running);
            var service = CreateService(battle);
            _repository.Save(service.Capture(BattleSaveSlot.Quicksave));
            var game = new LoadCommandStub();
            var actions = new BattleSaveActions(service, _repository, game, battle);
            _state.SetState(state);

            actions.QuickLoad();

            Assert.That(game.Loaded != null, Is.EqualTo(allowed));
            Assert.That(service.CanLoad, Is.EqualTo(allowed));
            if (!allowed) Assert.That(actions.Status, Does.StartWith("Load failed:"));
        }

        private sealed class LoadCommandStub : EmpireAtWar.Commands.Game.IGameCommand
        {
            public BattleSave Loaded { get; private set; }
            public void ExitGame() { }
            public void StartBattle(BattleDefinition definition) { }
            public void LoadBattle(BattleSave save) => Loaded = save;
        }

        private sealed class FakeCinematic : ICinematicReturnPose
        {
            public bool IsActive { get; set; }
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; } = Quaternion.identity;

            public bool TryGetReturnPose(out Vector3 position, out Quaternion rotation)
            {
                position = Position;
                rotation = Rotation;
                return IsActive;
            }
        }

        private sealed class FakeCamera : ICameraService
        {
            public FakeCamera(Transform transform, float zoomTargetHeight)
            {
                CameraTransform = transform;
                ZoomTargetHeight = zoomTargetHeight;
            }

            public string Id => nameof(FakeCamera);
            public Vector3 CameraPosition => CameraTransform.position;
            public Transform CameraTransform { get; }
            public Vector3 CameraForward => CameraTransform.forward;
            public float FieldOfView => 60f;
            public UnityEngine.Camera SceneCamera => null;
            public float ZoomTargetHeight { get; }

            public Vector3 GetWorldPoint(Vector2 point, Vector3 origin) => default;
            public RaycastHit ScreenPointToRay(Vector2 point) => default;
            public int ScreenPointRaycastAll(Vector2 point, RaycastHit[] results) => 0;
            public Vector3 WorldToViewportPoint(Vector3 point) => default;
            public Vector2 WorldToScreenPoint(Vector3 position) => default;
            public IReadOnlyList<Vector3> GetGroundFootprint(Vector2 min, Vector2 max) => Array.Empty<Vector3>();
            public void MoveTo(Vector3 point) { }
            public void SetPose(Vector3 position, Quaternion rotation) { }
            public float RestoredZoom { get; private set; }
            public void RestorePose(Vector3 position, Quaternion rotation, float zoomTargetHeight)
            {
                CameraTransform.SetPositionAndRotation(position, rotation);
                RestoredZoom = zoomTargetHeight;
            }
            public void SetCullingMask(LayerMask mask) { }
        }
    }
}
