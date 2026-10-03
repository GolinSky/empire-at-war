using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public sealed class PointerInput : IPointerInput, IInitializable, IDisposable
    {
        private readonly GameInputActions.PointerActions _pointer;

        public event Action<Vector2> PrimaryPressed;

        public event Action<Vector2> PrimaryReleased;

        public Vector2 Position => _pointer.Position.ReadValue<Vector2>();
        public int ClickCount => Mathf.Max(1, _pointer.ClickCount.ReadValue<int>());

        public PointerInput(InputActionsProvider provider)
        {
            _pointer = provider.Actions.Pointer;
        }

        public void Initialize()
        {
            _pointer.Primary.started += HandlePrimaryStarted;
            _pointer.Primary.canceled += HandlePrimaryCanceled;
        }

        public void Dispose()
        {
            _pointer.Primary.started -= HandlePrimaryStarted;
            _pointer.Primary.canceled -= HandlePrimaryCanceled;
        }

        private void HandlePrimaryStarted(InputAction.CallbackContext context)
        {
            PrimaryPressed?.Invoke(Position);
        }

        private void HandlePrimaryCanceled(InputAction.CallbackContext context)
        {
            PrimaryReleased?.Invoke(Position);
        }
    }
}
