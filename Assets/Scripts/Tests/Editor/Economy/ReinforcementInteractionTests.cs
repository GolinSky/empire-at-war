using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.SpawnArea;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
    [Category("BattleInput")]
    public sealed class ReinforcementInteractionTests
    {
        private GameObject _previewObject;
        private ReinforcementModel _model;
        private ReinforcementService _service;
        private FakeCancelInput _cancelInput;
        private UiCancelRouter _router;
        private FakeInputLock _inputLock;
        private FakeUiHitTest _hitTest;
        private FakeOverlay _overlay;
        private PlacementLock _placementLock;
        private int _pauseRequests;
        private int _failedPlacements;

        [SetUp]
        public void SetUp()
        {
            _previewObject = new GameObject("Reinforcement preview");
            _previewObject.SetActive(false);
            UnitSpawnView preview = _previewObject.AddComponent<UnitSpawnView>();
            _model = new ReinforcementModel(null) { IsTrySpawning = true };
            _cancelInput = new FakeCancelInput();
            _router = new UiCancelRouter(_cancelInput);
            _router.Initialize();
            _router.CancelUnhandled += () => _pauseRequests++;
            _inputLock = new FakeInputLock();
            _hitTest = new FakeUiHitTest();
            _overlay = new FakeOverlay();
            _placementLock = new PlacementLock();
            _service = new ReinforcementService(_inputLock, null, null, null,
                _overlay, _model, null, null, null, _hitTest, _router, null, null, null, null, null, null);
            ((List<UnitSpawnView>)typeof(ReinforcementService)
                .GetField("_previews", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_service)).Add(preview);
            SetField("_placementLock", _placementLock);
            _router.Focus(_service);
            _model.OnPlacementEnded += () => _failedPlacements++;
            _pauseRequests = 0;
            _failedPlacements = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _router.Dispose();
            Object.DestroyImmediate(_previewObject);
        }

        [Test]
        public void Escape_CancelsPlacementBeforePause_AndReleaseDoesNotDeploy()
        {
            ExpectPreviewDestroy();
            _cancelInput.Press();
            AssertCancelled();
            Assert.That(_pauseRequests, Is.Zero);

            Release();
            Assert.That(_failedPlacements, Is.EqualTo(1));
            _cancelInput.Press();
            Assert.That(_pauseRequests, Is.EqualTo(1));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Release_OverUiOrWhileModalLocked_CancelsWithoutProjectingIntoWorld(
            bool overUi, bool locked)
        {
            _hitTest.OverUi = overUi;
            _inputLock.IsLocked = locked;
            ExpectPreviewDestroy();
            Release();
            AssertCancelled();
            Assert.That(_service.TryCancel(), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Start_EmptyReserve_RejectsBeforeCreatingPreviewOrLockingInput(bool deployAll)
        {
            var model = new ReinforcementModel(null);
            int placementEnded = 0;
            model.OnPlacementEnded += () => placementEnded++;
            var service = new ReinforcementService(_inputLock, null, null, null,
                _overlay, model, null, null, null, _hitTest, _router, null, null,
                new FakeDeployInput(deployAll), null, null, null);
            var request = new MiningFacilityUnitRequest(null, default);

            service.TrySpawnReinforcement(request);

            Assert.That(model.IsTrySpawning, Is.False);
            Assert.That(service.TryCancel(), Is.False);
            Assert.That(placementEnded, Is.EqualTo(1));
            Assert.That(_inputLock.BattleLockRequests, Is.Zero);
        }

        [Test]
        public void Release_WhenWorldProjectionThrows_ReleasesPlacementBeforeRethrowing()
        {
            // The unavailable camera simulates failure before a deploy command can be submitted.
            ExpectPreviewDestroy();
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(Release);

            Assert.That(exception.InnerException, Is.TypeOf<NullReferenceException>());
            AssertCancelled();
            Assert.That(_service.TryCancel(), Is.False);
            _cancelInput.Press();
            Assert.That(_pauseRequests, Is.EqualTo(1));
        }

        [Test]
        public void ReleasingAim_KeepsPreviewPinned_UntilFacingReset()
        {
            var input = new FakeDeployInput(false);
            SetField("_deployInput", input);
            SetField("_placement", new FakeRotatablePlacement());
            typeof(UnitSpawnView).GetField("meshRenderers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_previewObject.GetComponent<UnitSpawnView>(), new MeshRenderer[0]);
            Vector3 anchor = Vector3.zero;
            Vector3 cursor = new Vector3(10f, 0f, 0f);

            input.IsAimHeld = true;
            Assert.That(UpdateFacing(anchor), Is.EqualTo(anchor));
            Assert.That(UpdateFacing(cursor), Is.EqualTo(anchor));

            // Releasing the aim button while the cursor is away must not snap the preview to it.
            input.IsAimHeld = false;
            Assert.That(UpdateFacing(cursor), Is.EqualTo(anchor));
            Assert.That(UpdateFacing(cursor + Vector3.forward), Is.EqualTo(anchor));

            // Pressing aim again only re-aims from the same pin.
            input.IsAimHeld = true;
            Assert.That(UpdateFacing(cursor), Is.EqualTo(anchor));
            input.IsAimHeld = false;

            input.WasFacingResetPressed = true;
            Assert.That(UpdateFacing(cursor), Is.EqualTo(cursor));
            input.WasFacingResetPressed = false;
            Assert.That(UpdateFacing(-cursor), Is.EqualTo(-cursor));
        }

        [Test]
        public void ReleasedPin_OnAreaThatBecameUnplaceable_FollowsCursorAgain()
        {
            var input = new FakeDeployInput(false);
            var placement = new FakeRotatablePlacement();
            SetField("_deployInput", input);
            SetField("_placement", placement);
            typeof(UnitSpawnView).GetField("meshRenderers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_previewObject.GetComponent<UnitSpawnView>(), new MeshRenderer[0]);
            Vector3 anchor = Vector3.zero;
            Vector3 cursor = new Vector3(10f, 0f, 0f);

            input.IsAimHeld = true;
            UpdateFacing(anchor);
            UpdateFacing(cursor);
            input.IsAimHeld = false;
            Assert.That(UpdateFacing(cursor), Is.EqualTo(anchor));

            // The pinned spot can no longer take the unit: the previews resume following the cursor.
            placement.BlockedPosition = anchor;
            Assert.That(UpdateFacing(cursor), Is.EqualTo(cursor));
            Assert.That(UpdateFacing(-cursor), Is.EqualTo(-cursor));
        }

        private Vector3 UpdateFacing(Vector3 cursor) => (Vector3)typeof(ReinforcementService)
            .GetMethod("UpdateFacing", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_service, new object[] { cursor });

        private void AssertCancelled()
        {
            Assert.That(_model.IsTrySpawning, Is.False);
            Assert.That(_placementLock.Disposed, Is.True);
            Assert.That(_overlay.Hidden, Is.True);
            Assert.That(_failedPlacements, Is.EqualTo(1));
        }

        private static void ExpectPreviewDestroy() =>
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));

        private void Release() => typeof(ReinforcementService)
            .GetMethod("Interrupt", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_service, new object[] { Vector2.zero });

        private void SetField(string name, object value) => typeof(ReinforcementService)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_service, value);

        private sealed class FakeCancelInput : ICancelInput
        {
            public event Action CancelPressed;
            public void Press() => CancelPressed.Invoke();
        }

        private sealed class FakeUiHitTest : IUiHitTest
        {
            public bool OverUi;
            public bool IsOverUi(Vector2 screenPosition) => OverUi;
        }

        private sealed class FakeInputLock : IInputLock
        {
            public event Action<bool> LockChanged { add { } remove { } }
            public bool IsLocked { get; set; }
            public int BattleLockRequests { get; private set; }
            public IDisposable Acquire() => throw new NotSupportedException();
            public IDisposable AcquireBattle()
            {
                BattleLockRequests++;
                throw new NotSupportedException();
            }
        }

        private sealed class FakeDeployInput : IReinforcementDeployInput
        {
            public bool IsDeployAllHeld { get; }
            public bool IsAimHeld { get; set; }
            public int TurnDirection => 0;
            public bool WasTurnPressed => false;
            public bool WasFacingResetPressed { get; set; }
            public FakeDeployInput(bool deployAll) => IsDeployAllHeld = deployAll;
        }

        private sealed class FakeRotatablePlacement : IReinforcementPlacement
        {
            public bool IsRotatable => true;
            public UnitSpawnView CreatePreview() => throw new NotSupportedException();
            public Quaternion GetAutoFacing(Vector3 position) => Quaternion.identity;
            public Vector3? BlockedPosition;
            public bool IsPositionValid(Vector3 position, Quaternion rotation) => position != BlockedPosition;
            public void Spawn(Vector3 position, Quaternion rotation) => throw new NotSupportedException();
        }

        private sealed class PlacementLock : IDisposable
        {
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        private sealed class FakeOverlay : ISpawnAreaOverlay
        {
            public bool Hidden;
            public void Show(PlayerId team, UnitRequest request, Func<Vector3, bool> isPositionValid) => Hidden = false;
            public Vector3 GetPlacementPosition(Vector3 position) => position;
            public float GetPlacementSpacing(float minimum) => minimum;
            public void Hide() => Hidden = true;
        }
    }
}
