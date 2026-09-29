using System;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    public interface IInputBindings
    {
        event Action BindingsChanged;

        string GetBindingDisplayString(InputAction action, int bindingIndex);

        /// <summary>Waits for the next key or button; Escape cancels. Composite parts are rebound by part index.</summary>
        void StartRebind(InputAction action, int bindingIndex, Action<RebindResult> completed);

        void ResetBinding(InputAction action, int bindingIndex);

        void ResetAll();
    }
}
