using System;
using UnityEngine.InputSystem;
using Zenject;

namespace EmpireAtWar.Services.Input
{
    public sealed class CancelInput : ICancelInput, IInitializable, IDisposable
    {
        private readonly GameInputActions.UiActions _ui;

        public event Action CancelPressed;

        public CancelInput(InputActionsProvider provider)
        {
            _ui = provider.Actions.Ui;
        }

        public void Initialize()
        {
            _ui.Cancel.performed += HandleCancel;
            _ui.Enable();
        }

        public void Dispose()
        {
            _ui.Cancel.performed -= HandleCancel;
            _ui.Disable();
        }

        private void HandleCancel(InputAction.CallbackContext context)
        {
            CancelPressed?.Invoke();
        }
    }
}
