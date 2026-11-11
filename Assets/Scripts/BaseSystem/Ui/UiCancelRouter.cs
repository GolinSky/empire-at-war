using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Ui.Base
{
    /// <summary>
    /// Gives the popup layer first chance to cancel, then dispatches to focused UIs.
    /// </summary>
    public sealed class UiCancelRouter : IUiCancelRouter, IUiPopupCancelLayer, IInitializable, IOrderedDisposable
    {
        private readonly ICancelInput _cancelInput;

        private readonly List<IUiCancelHandler> _focused = new List<IUiCancelHandler>();
        private IUiCancelHandler _popupLayer;

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

        public void SetPopupLayer(IUiCancelHandler handler) => _popupLayer = handler;

        private void Dispatch()
        {
            if (_popupLayer != null && _popupLayer.TryCancel())
            {
                return;
            }

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
