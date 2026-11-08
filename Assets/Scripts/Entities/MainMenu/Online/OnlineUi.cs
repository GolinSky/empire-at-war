using DG.Tweening;
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
        private const float INVITE_BLINK_DURATION = 0.5f;
        private IOnlinePresenter _presenter;
        private bool _isInitialized;
        private Tween _inviteBlink;
        private Color _inviteColor;
        private Color _inviteBlinkColor;

        public void AssignPresenter(IOnlinePresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            hostSkirmishButton.onClick.AddListener(_presenter.HostSkirmish);
            hostSurvivalButton.onClick.AddListener(_presenter.HostSurvival);
            joinButton.onClick.AddListener(Join);
            acceptInviteButton.onClick.AddListener(_presenter.AcceptInvite);
            backButton.onClick.AddListener(_presenter.Close);
            _inviteColor = acceptInviteButton.targetGraphic.color;
            _inviteBlinkColor = new Color(1f, 0.12f, 0.12f, _inviteColor.a);
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
            acceptInviteButton.interactable = !busy;
            acceptInviteLabel.text = $"JOIN {inviteFrom.ToUpperInvariant()}";
            SetInviteBlinking(inviteFrom.Length > 0);
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            hostSkirmishButton.onClick.RemoveListener(_presenter.HostSkirmish);
            hostSurvivalButton.onClick.RemoveListener(_presenter.HostSurvival);
            joinButton.onClick.RemoveListener(Join);
            acceptInviteButton.onClick.RemoveListener(_presenter.AcceptInvite);
            backButton.onClick.RemoveListener(_presenter.Close);
            SetInviteBlinking(false);
            _isInitialized = false;
        }

        // A pending invite blinks red so the player notices it.
        private void SetInviteBlinking(bool blinking)
        {
            if (blinking)
            {
                if (_inviteBlink != null) return;
                acceptInviteButton.targetGraphic.color = _inviteColor;
                _inviteBlink = acceptInviteButton.targetGraphic
                    .DOColor(_inviteBlinkColor, INVITE_BLINK_DURATION)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
                return;
            }

            if (_inviteBlink == null) return;
            _inviteBlink.Kill();
            _inviteBlink = null;
            acceptInviteButton.targetGraphic.color = _inviteColor;
        }

        private void Join() => _presenter.Join(addressInput.text);

        private void OnDestroy() => Dispose();
    }
}
