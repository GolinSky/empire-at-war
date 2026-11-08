using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Services.Networking.Online;
using UnityEngine;

namespace EmpireAtWar.Entities.OnlineMenu
{
    /// <summary>
    /// Interim online menu and lobby (IMGUI) until the uGUI lobby prefab exists: Host Skirmish / Host Survival /
    /// Join by IP, members with ready ticks, kick, in-game Steam friend invites and Force Start. Battle setup reuses
    /// the Skirmish and Survival screens; their Start button starts the online match while hosting.
    /// </summary>
    public sealed class OnlineMenuPanel : MonoBehaviour
    {
        private const float WIDTH = 340f;

        private OnlineMatchService _online;
        private SkirmishRouteUiController _skirmish;
        private SurvivalRouteUiController _survival;
        private bool _isOpen;
        private bool _ready;
        private bool _viaSteam = true;
        private bool _hostSurvival;
        private string _address = "localhost";
        private Vector2 _scroll;

        public void Bind(OnlineMatchService online, SkirmishRouteUiController skirmish, SurvivalRouteUiController survival)
        {
            _online = online;
            _skirmish = skirmish;
            _survival = survival;
            _isOpen = online.Role != OnlineRole.None || online.LastError != OnlineError.None;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - WIDTH - 16f, 16f, WIDTH, Screen.height - 32f), GUI.skin.box);
            if (!_isOpen)
            {
                if (GUILayout.Button("Online")) _isOpen = true;
            }
            else
            {
                DrawError();
                if (_online.Role == OnlineRole.None) DrawEntry();
                else DrawLobby();
            }

            GUILayout.EndArea();
        }

        private void DrawError()
        {
            if (_online.LastError == OnlineError.None) return;
            GUILayout.Label($"Error: {Describe(_online.LastError)}");
            if (GUILayout.Button("OK")) _online.ClearError();
        }

        private void DrawEntry()
        {
            GUILayout.Label($"Steam: {_online.Steam.Status}");
            _viaSteam = GUILayout.Toggle(_viaSteam, "Host through Steam (friends invite)");
            if (GUILayout.Button("Host Skirmish")) Host(false);
            if (GUILayout.Button("Host Survival")) Host(true);
            GUILayout.Space(8f);
            GUILayout.Label("Join by IP");
            _address = GUILayout.TextField(_address);
            if (GUILayout.Button("Join")) _online.JoinByIp(_address);
            if (_online.Steam.HasPendingInvite && GUILayout.Button($"Accept invite from {_online.Steam.PendingInviteFrom}"))
                _online.Steam.AcceptInvite();
            if (GUILayout.Button("Close")) _isOpen = false;
        }

        private void DrawLobby()
        {
            bool isHost = _online.Role == OnlineRole.Host;
            GUILayout.Label(isHost ? $"Hosting {(_hostSurvival ? "Survival" : "Skirmish")}" : "In lobby");
            foreach (LobbyMember member in _online.Members)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{member.Name} {(member.Ready ? "[ready]" : "[...]")}{(member.Connected ? "" : " (away)")}");
                if (isHost && member.ConnectionId != OnlineLobby.HOST_CONNECTION_ID && GUILayout.Button("Kick", GUILayout.Width(50f)))
                    _online.Kick(member.ConnectionId);
                GUILayout.EndHorizontal();
            }

            if (isHost) DrawHostControls();
            else if (GUILayout.Toggle(_ready, "Ready") != _ready)
            {
                _ready = !_ready;
                _online.SetReady(_ready);
            }

            if (GUILayout.Button("Leave")) _online.Leave();
        }

        private void DrawHostControls()
        {
            if (GUILayout.Button("Battle setup")) OpenSetup();
            if (GUILayout.Button(_online.CanStart ? "Start" : "Force Start"))
            {
                _online.ArmForceStart();
                if (_hostSurvival) _survival.StartBattle();
                else _skirmish.StartGame();
            }

            if (!_online.Steam.InLobby) return;
            GUILayout.Label("Invite friends");
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(160f));
            foreach (SteamFriend friend in _online.Steam.RefreshOnlineFriends())
                if (GUILayout.Button(friend.Name)) _online.Steam.InviteFriend(friend);
            GUILayout.EndScrollView();
        }

        private void Host(bool survival)
        {
            _hostSurvival = survival;
            _online.HostLobby(_viaSteam);
            if (_online.Role == OnlineRole.Host) OpenSetup();
        }

        private void OpenSetup()
        {
            if (_hostSurvival) _survival.Open();
            else _skirmish.Open();
        }

        private static string Describe(OnlineError error) => error switch
        {
            OnlineError.SteamNotRunning => "Steam is not running.",
            OnlineError.LobbyFull => "The lobby is full.",
            OnlineError.VersionMismatch => "Game versions differ.",
            OnlineError.HostLeft => "The host left.",
            OnlineError.Kicked => "You were removed from the lobby.",
            OnlineError.BattleInProgress => "That battle is already running.",
            OnlineError.NotEnoughSlots => "Not enough player slots: add an AI slot for every lobby member.",
            OnlineError.SurvivalCoopNeedsLargerMap => "Survival co-op needs a Medium or Large map.",
            OnlineError.SurvivalLobbyTooLarge => "Survival co-op holds at most 3 players.",
            OnlineError.ConnectionFailed => "Could not connect.",
            _ => throw new System.ArgumentOutOfRangeException(nameof(error), error, "No text for this error.")
        };
    }
}
