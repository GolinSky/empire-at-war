using System;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Popups
{
    /// <summary>Owns the accept/decline popup view. Cancel (Escape) declines.</summary>
    public sealed class AcceptPopupUiController : UiController, IPopupController, IAcceptPopupPresenter,
        ILateDisposable
    {
        private AcceptPopupUi _ui;
        private Action<bool> _answered;

        public AcceptPopupUiController(IUiService uiService, IUiCancelRouter cancelRouter)
            : base(uiService, cancelRouter)
        {
        }

        public void Show(AcceptPopupContent content, Action<bool> answered)
        {
            if (_answered != null)
            {
                throw new InvalidOperationException("An accept popup is already open.");
            }

            if (_ui == null)
            {
                _ui = CreateUi<AcceptPopupUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
            }

            _answered = answered;
            _ui.Render(content);
            _ui.Show();
            Focus();
        }

        public void Accept() => Answer(true);

        public void Decline() => Answer(false);

        protected override bool HandleCancel()
        {
            Decline();
            return true;
        }

        public void LateDispose()
        {
            Unfocus();
            if (_ui != null) _ui.Dispose();
            _ui = null;
            _answered = null;
        }

        private void Answer(bool isAccepted)
        {
            Action<bool> answered = _answered;
            _answered = null;
            _ui.Hide();
            Unfocus();
            answered(isAccepted);
        }
    }
}
