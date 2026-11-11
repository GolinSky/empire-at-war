using System;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Popups
{
    public sealed class AcceptPopupUiController : IPopupController, IAcceptPopupPresenter
    {
        private readonly IUiService _uiService;
        private readonly AcceptPopupContent _content;
        private readonly Action<ConfirmPopupResult> _answered;
        private AcceptPopupUi _ui;
        private Action _complete;
        private ConfirmPopupResult _result;

        public AcceptPopupUiController(IUiService uiService, AcceptPopupContent content,
            Action<ConfirmPopupResult> answered)
        {
            _uiService = uiService;
            _content = content;
            _answered = answered;
        }

        public BaseUi Open(Action complete)
        {
            _complete = complete;
            _ui = _uiService.CreateUi<AcceptPopupUi>();
            _ui.AssignPresenter(this);
            _ui.Render(_content);
            _ui.Initialize();
            _ui.Show();
            return _ui;
        }

        public void SetCovered(bool covered)
        {
        }

        public void Accept() => Answer(ConfirmPopupResult.Accepted);
        public void Decline() => Answer(ConfirmPopupResult.Declined);
        public void Cancel() => Decline();

        public void Dispose(bool dismissed)
        {
            _ui.Dispose();
            _answered(dismissed ? ConfirmPopupResult.Dismissed : _result);
        }

        private void Answer(ConfirmPopupResult result)
        {
            _result = result;
            _complete();
        }
    }
}
