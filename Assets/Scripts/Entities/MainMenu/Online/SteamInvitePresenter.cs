using System.Collections.Generic;
using EmpireAtWar.Services.Networking.Online;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    public sealed class SteamInvitePresenter : ISteamInvitePresenter
    {
        private readonly SteamPlatform _steam;
        private readonly SteamInviteView _view;
        private readonly List<SteamFriend> _friends = new List<SteamFriend>();

        public SteamInvitePresenter(SteamPlatform steam, SteamInviteView view)
        {
            _steam = steam;
            _view = view;
        }

        public void Initialize()
        {
            _view.Initialize(this);
            _steam.AvatarLoaded += UpdateAvatars;
        }

        public void Dispose()
        {
            _steam.AvatarLoaded -= UpdateAvatars;
            _view.Dispose();
        }

        public void Open()
        {
            Refresh();
            _steam.InviteFriends();
            if (_friends.Count > 0) _view.ShowStatus(_steam.Status);
        }

        public void Refresh()
        {
            _friends.Clear();
            _friends.AddRange(_steam.RefreshOnlineFriends());
            var names = new List<string>(_friends.Count);
            foreach (SteamFriend friend in _friends) names.Add(friend.Name);
            _view.Show(names, names.Count > 0 ? "Choose an online friend, then press INVITE."
                : "No friends are online. Refresh after your friend signs in to Steam.");
            UpdateAvatars();
        }

        private void UpdateAvatars()
        {
            if (!_view.IsOpen) return;
            bool changed = false;
            for (int index = 0; index < _friends.Count; index++)
                if (_steam.TryGetAvatar(_friends[index].Id.m_SteamID, out var avatar))
                    changed |= _view.SetAvatar(index, avatar);
            if (changed) _view.RefreshAvatars();
        }

        public void Invite(int index)
        {
            if (_steam.InviteFriend(_friends[index]))
                Close();
            else
                _view.ShowStatus(_steam.Status);
        }

        public void Close() => _view.Hide();

        public bool TryClose()
        {
            if (!_view.IsOpen) return false;
            Close();
            return true;
        }
    }
}
