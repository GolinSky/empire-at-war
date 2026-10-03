using System;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public sealed class InputLockService : IInputLock, IInitializable, IDisposable
    {
        private readonly GameInputActions _actions;

        private int _lockCount;

        public event Action<bool> LockChanged;

        public bool IsLocked => _lockCount > 0;

        public InputLockService(InputActionsProvider provider)
        {
            _actions = provider.Actions;
        }

        public void Initialize()
        {
            _actions.Pointer.Enable();
            _actions.Camera.Enable();
            _actions.Battle.Enable();
        }

        public void Dispose()
        {
            _actions.Pointer.Disable();
            _actions.Camera.Disable();
            _actions.Battle.Disable();
        }

        public IDisposable Acquire()
        {
            _lockCount++;
            if (_lockCount == 1)
            {
                SetGameplayEnabled(false);
            }

            return new LockHandle(this);
        }

        private void Release()
        {
            _lockCount--;
            if (_lockCount == 0)
            {
                SetGameplayEnabled(true);
            }
        }

        private void SetGameplayEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                _actions.Camera.Enable();
                _actions.Battle.Enable();
            }
            else
            {
                _actions.Camera.Disable();
                _actions.Battle.Disable();
            }

            LockChanged?.Invoke(!isEnabled);
        }

        private sealed class LockHandle : IDisposable
        {
            private readonly InputLockService _owner;

            private bool _isReleased;

            public LockHandle(InputLockService owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_isReleased)
                {
                    return;
                }

                _isReleased = true;
                _owner.Release();
            }
        }
    }
}
