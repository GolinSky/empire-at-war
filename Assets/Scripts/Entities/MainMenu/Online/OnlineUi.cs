using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    /// <summary>Online entry: host a Skirmish or Survival lobby, join by IP, accept a Steam invite.</summary>
    public sealed class OnlineUi : BaseUi
    {
        [SerializeField] private Button hostSkirmishButton;
        [SerializeField] private Button hostSurvivalButton;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button acceptInviteButton;
        [SerializeField] private TMP_Text acceptInviteLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text statusText;
        private IOnlinePresenter _presenter;
        private bool _isInitialized;

        public void AssignPresenter(IOnlinePresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            hostSkirmishButton.onClick.AddListener(_presenter.HostSkirmish);
            hostSurvivalButton.onClick.AddListener(_presenter.HostSurvival);
            joinButton.onClick.AddListener(Join);
            acceptInviteButton.onClick.AddListener(_presenter.AcceptInvite);
            backButton.onClick.AddListener(_presenter.Close);
            _isInitialized = true;
        }

        /// <param name="busy">Connecting to a host: hosting and joining wait; Back cancels.</param>
        public void Render(string status, bool busy, string inviteFrom)
        {
            statusText.text = status;
            hostSkirmishButton.interactable = !busy;
            hostSurvivalButton.interactable = !busy;
            joinButton.interactable = !busy;
            addressInput.interactable = !busy;
            acceptInviteButton.gameObject.SetActive(inviteFrom.Length > 0);
            acceptInviteLabel.text = $"JOIN {inviteFrom.ToUpperInvariant()}";
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            hostSkirmishButton.onClick.RemoveListener(_presenter.HostSkirmish);
            hostSurvivalButton.onClick.RemoveListener(_presenter.HostSurvival);
            joinButton.onClick.RemoveListener(Join);
            acceptInviteButton.onClick.RemoveListener(_presenter.AcceptInvite);
            backButton.onClick.RemoveListener(_presenter.Close);
            _isInitialized = false;
        }

        private void Join() => _presenter.Join(addressInput.text);

        private void OnDestroy() => Dispose();
    }
}
