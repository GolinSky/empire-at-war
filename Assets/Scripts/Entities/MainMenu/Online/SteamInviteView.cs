using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    public sealed class SteamInviteView : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown friends;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button inviteButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button closeButton;
        private ISteamInvitePresenter _presenter;
        private bool _initialized;

        public bool IsOpen => gameObject.activeSelf;

        public void Initialize(ISteamInvitePresenter presenter)
        {
            _presenter = presenter;
            inviteButton.onClick.AddListener(Invite);
            refreshButton.onClick.AddListener(_presenter.Refresh);
            closeButton.onClick.AddListener(_presenter.Close);
            _initialized = true;
            Hide();
        }

        public void Show(List<string> names, string message)
        {
            friends.ClearOptions();
            friends.AddOptions(names.Count > 0 ? names : new List<string> { "No online friends" });
            friends.SetValueWithoutNotify(0);
            friends.interactable = names.Count > 0;
            inviteButton.interactable = names.Count > 0;
            status.text = message;
            gameObject.SetActive(true);
        }

        public void ShowStatus(string message) => status.text = message;

        public void Hide()
        {
            friends.Hide();
            gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (!_initialized) return;
            inviteButton.onClick.RemoveListener(Invite);
            refreshButton.onClick.RemoveListener(_presenter.Refresh);
            closeButton.onClick.RemoveListener(_presenter.Close);
            _initialized = false;
        }

        private void Invite() => _presenter.Invite(friends.value);
        private void OnDestroy() => Dispose();
    }
}
