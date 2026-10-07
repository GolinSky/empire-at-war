using System;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public sealed class InputLockService : IInputLock, IInitializable, IDisposable
    {
        private readonly GameInputActions _actions;

        private int _lockCount;
        private int _battleLockCount;

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

            return new LockHandle(Release);
        }

        public IDisposable AcquireBattle()
        {
            _battleLockCount++;
            UpdateBattleActions();
            return new LockHandle(ReleaseBattle);
        }

        private void Release()
        {
            _lockCount--;
            if (_lockCount == 0)
            {
                SetGameplayEnabled(true);
            }
        }

        private void ReleaseBattle()
        {
            _battleLockCount--;
            UpdateBattleActions();
        }

        private void SetGameplayEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                _actions.Camera.Enable();
            }
            else
            {
                _actions.Camera.Disable();
            }

            UpdateBattleActions();
            LockChanged?.Invoke(!isEnabled);
        }

        private void UpdateBattleActions()
        {
            if (_lockCount == 0 && _battleLockCount == 0)
            {
                _actions.Battle.Enable();
            }
            else
            {
                _actions.Battle.Disable();
            }
        }

        private sealed class LockHandle : IDisposable
        {
            private readonly Action _release;

            private bool _isReleased;

            public LockHandle(Action release)
            {
                _release = release;
            }

            public void Dispose()
            {
                if (_isReleased)
                {
                    return;
                }

                _isReleased = true;
                _release();
            }
        }
    }
}
