using System;
using System.Reflection;
using System.Text.RegularExpressions;
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
                _overlay, _model, null, null, null, _hitTest, _router, null, null);
            SetField("_preview", preview);
            SetField("_placementLock", _placementLock);
            _router.Focus(_service);
            _model.OnSpawnUnit += success =>
            {
                Assert.That(success, Is.False);
                _failedPlacements++;
            };
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
            public IDisposable Acquire() => throw new NotSupportedException();
            public IDisposable AcquireBattle() => throw new NotSupportedException();
        }

        private sealed class PlacementLock : IDisposable
        {
            public bool Disposed;
            public void Dispose() => Disposed = true;
        }

        private sealed class FakeOverlay : ISpawnAreaOverlay
        {
            public bool Hidden;
            public void Show(PlayerId team) => Hidden = false;
            public void Hide() => Hidden = true;
        }
    }
}
