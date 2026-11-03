using System;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Popups
{
    public class AcceptPopupUi : BaseUi
    {
        private IAcceptPopupPresenter _presenter;

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private TMP_Text acceptLabel;
        [SerializeField] private Button declineButton;
        [SerializeField] private TMP_Text declineLabel;

        private bool _isInitialized;

        public void AssignPresenter(IAcceptPopupPresenter presenter)
        {
            if (_presenter != null)
            {
                throw new InvalidOperationException("AcceptPopupUi presenter is already assigned.");
            }
            _presenter = presenter;
        }

        public void Render(AcceptPopupContent content)
        {
            titleText.text = content.Title;
            messageText.text = content.Message;
            acceptLabel.text = content.AcceptLabel;
            declineLabel.text = content.DeclineLabel;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            acceptButton.onClick.AddListener(_presenter.Accept);
            declineButton.onClick.AddListener(_presenter.Decline);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            acceptButton.onClick.RemoveListener(_presenter.Accept);
            declineButton.onClick.RemoveListener(_presenter.Decline);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
