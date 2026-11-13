using DG.Tweening;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    /// <summary>Multiplayer entry: host a Steam or LAN / IP lobby, open the Server Browser, accept a Steam invite, retry Steam.</summary>
    public sealed class OnlineUi : BaseUi
    {
        [FormerlySerializedAs("hostGameButton")]
        [SerializeField] private Button hostSteamButton;
        [SerializeField] private Button hostLanButton;
        [SerializeField] private Button joinGameButton;
        [SerializeField] private Button acceptInviteButton;
        [SerializeField] private TMP_Text acceptInviteLabel;
        [SerializeField] private Button retrySteamButton;
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
            hostSteamButton.onClick.AddListener(_presenter.HostSteamLobby);
            hostLanButton.onClick.AddListener(_presenter.HostLanGame);
            joinGameButton.onClick.AddListener(_presenter.OpenServerBrowser);
            acceptInviteButton.onClick.AddListener(_presenter.AcceptInvite);
            retrySteamButton.onClick.AddListener(_presenter.RetrySteam);
            backButton.onClick.AddListener(_presenter.Close);
            _inviteColor = acceptInviteButton.targetGraphic.color;
            _inviteBlinkColor = new Color(210f / 255f, 169f / 255f, 94f / 255f, _inviteColor.a);
            _isInitialized = true;
        }

        /// <param name="busy">Connecting to a host: hosting and joining wait; Back cancels.</param>
        /// <param name="steamReady">Without Steam the Steam lobby is blocked and Retry Steam takes the invite slot.</param>
        public void Render(string status, bool busy, string inviteFrom, bool steamReady)
        {
            statusText.text = status;
            hostSteamButton.interactable = steamReady && !busy;
            hostLanButton.interactable = !busy;
            joinGameButton.interactable = !busy;
            retrySteamButton.gameObject.SetActive(!steamReady);
            retrySteamButton.interactable = !busy;
            acceptInviteButton.gameObject.SetActive(inviteFrom.Length > 0);
            acceptInviteButton.interactable = !busy;
            acceptInviteLabel.text = $"JOIN {inviteFrom.ToUpperInvariant()}";
            SetInviteBlinking(inviteFrom.Length > 0);
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            hostSteamButton.onClick.RemoveListener(_presenter.HostSteamLobby);
            hostLanButton.onClick.RemoveListener(_presenter.HostLanGame);
            joinGameButton.onClick.RemoveListener(_presenter.OpenServerBrowser);
            acceptInviteButton.onClick.RemoveListener(_presenter.AcceptInvite);
            retrySteamButton.onClick.RemoveListener(_presenter.RetrySteam);
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

        private void OnDestroy() => Dispose();
    }
}
