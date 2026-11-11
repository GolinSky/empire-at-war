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

        public void Initialize(ISteamInvitePresenter presenter)
        {
            _presenter = presenter;
            inviteButton.onClick.AddListener(Invite);
            refreshButton.onClick.AddListener(_presenter.Refresh);
            closeButton.onClick.AddListener(_presenter.Close);
            _initialized = true;
        }

        public void Render(List<string> names, string message)
        {
            friends.Hide();
            ReleaseAvatars();
            friends.ClearOptions();
            friends.AddOptions(names.Count > 0 ? names : new List<string> { "No online friends" });
            friends.SetValueWithoutNotify(0);
            friends.interactable = names.Count > 0;
            inviteButton.interactable = names.Count > 0;
            status.text = message;
        }

        public void ShowStatus(string message) => status.text = message;

        public bool SetAvatar(int index, Texture2D avatar)
        {
            TMP_Dropdown.OptionData option = friends.options[index];
            if (option.image != null) return false;
            option.image = Sprite.Create(avatar, new Rect(0, 0, avatar.width, avatar.height),
                new Vector2(0.5f, 0.5f));
            return true;
        }

        public void RefreshAvatars()
        {
            friends.RefreshShownValue();
            if (!friends.enabled || !friends.IsExpanded) return;
            // Hide fades asynchronously, so Show would still see the previous list.
            friends.enabled = false;
            friends.enabled = true;
            friends.Show();
        }

        private void ReleaseAvatars()
        {
            foreach (TMP_Dropdown.OptionData option in friends.options)
            {
                if (option.image == null) continue;
                if (Application.isPlaying)
                    Destroy(option.image);
                else
                    DestroyImmediate(option.image);
                option.image = null;
            }
        }

        // TMP_Dropdown.OnDisable immediately collapses its independently sorted list and blocker.
        public void SetCovered(bool covered) => friends.enabled = !covered;

        public void Dispose()
        {
            if (!_initialized) return;
            SetCovered(true);
            ReleaseAvatars();
            inviteButton.onClick.RemoveListener(Invite);
            refreshButton.onClick.RemoveListener(_presenter.Refresh);
            closeButton.onClick.RemoveListener(_presenter.Close);
            _initialized = false;
        }

        private void Invite() => _presenter.Invite(friends.value);
        private void OnDestroy() => Dispose();
    }
}
