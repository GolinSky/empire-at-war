using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Popups
{
    /// <summary>Shows popups one at a time; requests made while one is open wait in order.</summary>
    public sealed class PopupService : IPopupService
    {
        private readonly IPopupController _acceptPopup;
        private readonly Queue<Action> _pending = new Queue<Action>();
        private bool _isShowing;

        public PopupService(IPopupController acceptPopup)
        {
            _acceptPopup = acceptPopup;
        }

        public void ShowAccept(AcceptPopupContent content, Action<bool> answered)
        {
            _pending.Enqueue(() => _acceptPopup.Show(content, isAccepted =>
            {
                answered(isAccepted);
                ShowNext();
            }));

            if (!_isShowing)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            _isShowing = _pending.Count > 0;
            if (_isShowing)
            {
                _pending.Dequeue()();
            }
        }
    }
}
