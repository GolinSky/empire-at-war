using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Input;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    /// <summary>
    /// Sends each cancel request to the focused UIs, most recently focused first, until one consumes it.
    /// </summary>
    public sealed class UiCancelRouter : IUiCancelRouter, IInitializable, IDisposable
    {
        private readonly ICancelInput _cancelInput;
        private readonly List<IUiCancelHandler> _focused = new List<IUiCancelHandler>();

        public event Action CancelUnhandled;

        public UiCancelRouter(ICancelInput cancelInput)
        {
            _cancelInput = cancelInput;
        }

        public void Initialize()
        {
            _cancelInput.CancelPressed += Dispatch;
        }

        public void Dispose()
        {
            _cancelInput.CancelPressed -= Dispatch;
            _focused.Clear();
        }

        public void Focus(IUiCancelHandler handler)
        {
            _focused.Remove(handler);
            _focused.Add(handler);
        }

        public void Unfocus(IUiCancelHandler handler)
        {
            _focused.Remove(handler);
        }

        private void Dispatch()
        {
            for (int i = _focused.Count - 1; i >= 0; i--)
            {
                if (_focused[i].TryCancel())
                {
                    return;
                }
            }

            CancelUnhandled?.Invoke();
        }
    }
}
