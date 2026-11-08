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

        public void Initialize() => _view.Initialize(this);
        public void Dispose() => _view.Dispose();

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
        }

        public void Invite(int index)
        {
            _steam.InviteFriend(_friends[index]);
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
