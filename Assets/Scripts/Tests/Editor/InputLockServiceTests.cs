using System;
using EmpireAtWar.Services.Input;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class InputLockServiceTests
    {
        private InputActionsProvider _provider;
        private InputLockService _lock;

        [SetUp]
        public void SetUp()
        {
            _provider = new InputActionsProvider();
            _lock = new InputLockService(_provider);
            _lock.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _lock.Dispose();
            _provider.Dispose();
        }

        [Test]
        public void Lock_StaysUntilEveryHandleIsReleased()
        {
            IDisposable first = _lock.Acquire();
            IDisposable second = _lock.Acquire();
            first.Dispose();

            Assert.That(_lock.IsLocked, Is.True);
            Assert.That(_provider.Actions.Battle.enabled, Is.False);
            Assert.That(_provider.Actions.Camera.enabled, Is.False);
            Assert.That(_provider.Actions.Pointer.enabled, Is.True);

            second.Dispose();

            Assert.That(_lock.IsLocked, Is.False);
            Assert.That(_provider.Actions.Battle.enabled, Is.True);
            Assert.That(_provider.Actions.Camera.enabled, Is.True);
        }

        [Test]
        public void ReleasingHandleTwice_DoesNotReleaseAnotherLock()
        {
            IDisposable first = _lock.Acquire();
            IDisposable second = _lock.Acquire();
            first.Dispose();
            first.Dispose();

            Assert.That(_lock.IsLocked, Is.True);
            second.Dispose();
        }
    }
}
