using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Saves;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleSaveServiceTests
    {
        private GameObject _cameraObject;
        private FakeCamera _camera;
        private SkirmishStateModel _state;
        private string _directory;
        private JsonBattleSaveRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject(nameof(BattleSaveServiceTests));
            _cameraObject.transform.SetPositionAndRotation(new Vector3(-120f, 600f, 75f), Quaternion.Euler(60f, -35f, 0f));
            _camera = new FakeCamera(_cameraObject.transform, 540f);
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
                    clock, _repository, units, Array.Empty<IBattleSaveSource>());
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

        private BattleSaveService CreateService(BattleDefinition battle) =>
            new BattleSaveService(battle, _state, _camera, new BattleClock(_state), _repository,
                new UnitSaveService(new EntityLocator(), new BattleUnitSpawner(), null), Array.Empty<IBattleSaveSource>());

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
