using System;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Controllers.Game;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class InputLockServiceTests
    {
        private InputActionsProvider _provider;
        private InputLockService _inputLockService;

        [SetUp]
        public void SetUp()
        {
            _provider = new InputActionsProvider();
            _inputLockService = new InputLockService(_provider);
            _inputLockService.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _inputLockService.Dispose();
            // GameInputActions.Dispose uses Object.Destroy, which Unity rejects in Edit Mode.
            UnityEngine.Object.DestroyImmediate(_provider.Actions.asset);
        }

        [Test]
        public void Lock_StaysUntilEveryHandleIsReleased()
        {
            IDisposable first = _inputLockService.Acquire();
            IDisposable second = _inputLockService.Acquire();
            first.Dispose();

            Assert.That(_inputLockService.IsLocked, Is.True);
            Assert.That(_provider.Actions.Battle.enabled, Is.False);
            Assert.That(_provider.Actions.Camera.enabled, Is.False);
            Assert.That(_provider.Actions.Pointer.enabled, Is.True);

            second.Dispose();

            Assert.That(_inputLockService.IsLocked, Is.False);
            Assert.That(_provider.Actions.Battle.enabled, Is.True);
            Assert.That(_provider.Actions.Camera.enabled, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EndedBattle_QuickLoadSurvivesInputLockAndUnsubscribesOnTeardown(bool lockFirst)
        {
            SkirmishStateModel state = new SkirmishStateModel();
            BattleSaveInput saves = new BattleSaveInput(_provider, null, state, _inputLockService);
            saves.Initialize();
            IDisposable inputLock = null;
            try
            {
                if (lockFirst) inputLock = _inputLockService.Acquire();
                state.SetState(BattleState.Ended);
                if (!lockFirst) inputLock = _inputLockService.Acquire();
                Assert.That(_provider.Actions.Battle.QuickLoad.enabled, Is.True);
                Assert.That(_provider.Actions.Battle.QuickSave.enabled, Is.False);
                Assert.That(_provider.Actions.Camera.enabled, Is.False);
                saves.LateDispose();
                Assert.That(_provider.Actions.Battle.QuickLoad.enabled, Is.False);
                state.SetState(BattleState.Ended);
                Assert.That(_provider.Actions.Battle.QuickLoad.enabled, Is.False);
            }
            finally { inputLock.Dispose(); }
        }

        [Test]
        public void BattleLock_KeepsCameraEnabled()
        {
            IDisposable battleLock = _inputLockService.AcquireBattle();

            Assert.That(_inputLockService.IsLocked, Is.False);
            Assert.That(_provider.Actions.Battle.enabled, Is.False);
            Assert.That(_provider.Actions.Camera.enabled, Is.True);

            battleLock.Dispose();

            Assert.That(_provider.Actions.Battle.enabled, Is.True);
        }

        [Test]
        public void ReleasingFullLock_KeepsBattleLockedWhileBattleLockIsHeld()
        {
            IDisposable battleLock = _inputLockService.AcquireBattle();
            _inputLockService.Acquire().Dispose();

            Assert.That(_provider.Actions.Battle.enabled, Is.False);
            Assert.That(_provider.Actions.Camera.enabled, Is.True);

            battleLock.Dispose();
        }

        [Test]
        public void ReleasingHandleTwice_DoesNotReleaseAnotherLock()
        {
            IDisposable first = _inputLockService.Acquire();
            IDisposable second = _inputLockService.Acquire();
            first.Dispose();
            first.Dispose();

            Assert.That(_inputLockService.IsLocked, Is.True);
            second.Dispose();
        }
    }
}
