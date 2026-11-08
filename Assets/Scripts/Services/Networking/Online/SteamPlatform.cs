using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Online
{
    // Steam lobby flow (from the Phase 0 spike): init, friends-only lobby, overlay invite, join request, launch arg.
    // App ID 480 (Spacewar) shares its lobby namespace, so lobbies carry game + version data and joins check it.
    public sealed class SteamPlatform
    {
        public const string GAME_KEY = "empire-at-war";
        private const string GAME_DATA = "game";
        private const string VERSION_DATA = "version";
        private const string HOST_DATA = "host";
        private const string CONNECT_LOBBY_ARG = "+connect_lobby";
        private const string CONNECT_PRESENCE = "connect";
        private const string STATUS_PRESENCE = "status";
        private const int MAX_MEMBERS = 4;
        /// <summary>Spacewar while developing; replace with the game's own App ID.</summary>
        private const string STEAM_APP_ID = "480";

        private CallResult<LobbyCreated_t> _lobbyCreated;
        private readonly Dictionary<CSteamID, CallResult<LobbyEnter_t>> _lobbyJoins =
            new Dictionary<CSteamID, CallResult<LobbyEnter_t>>();
        private Callback<GameLobbyJoinRequested_t> _joinRequested;
        private Callback<LobbyInvite_t> _lobbyInvite;
        private Callback<GameRichPresenceJoinRequested_t> _presenceJoinRequested;
        private Callback<AvatarImageLoaded_t> _avatarLoaded;
        private readonly System.Collections.Generic.Dictionary<ulong, Texture2D> _avatars =
            new System.Collections.Generic.Dictionary<ulong, Texture2D>();
        private readonly List<SteamFriend> _friends = new List<SteamFriend>();
        private CSteamID _lobby = CSteamID.Nil;
        private CSteamID _requestedLobby = CSteamID.Nil;
        private bool _isHost;

        public event Action<string> HostLobbyReady;
        public event Action<string> JoinedHost;
        public event Action JoinStarting;
        public event Action<OnlineError> Failed;
        /// <summary>An in-game lobby invite arrived (<see cref="HasPendingInvite"/>).</summary>
        public event Action InviteReceived;
        /// <summary>A friend's avatar finished downloading; <see cref="TryGetAvatar"/> now has it.</summary>
        public event Action AvatarLoaded;

        public bool IsInitialized { get; private set; }
        private string _status = "Steam not initialized";

        public string PersonaName => IsInitialized ? SteamFriends.GetPersonaName() : System.Environment.UserName;
        /// <summary>0 without Steam (direct-IP play): no avatar.</summary>
        public ulong LocalSteamId => IsInitialized ? SteamUser.GetSteamID().m_SteamID : 0UL;

        public string Status
        {
            get => _status;
            private set
            {
                _status = value;
                Debug.Log($"[Online][Steam] {value}");
            }
        }
        public bool InLobby => _lobby != CSteamID.Nil;
        public bool IsJoiningLobby => _requestedLobby != CSteamID.Nil;
        public ulong LobbyId => _lobby.m_SteamID;
        /// <summary>Invite received in-game (no overlay needed); accept with <see cref="AcceptInvite"/>.</summary>
        public bool HasPendingInvite => PendingInviteLobby != CSteamID.Nil;
        public CSteamID PendingInviteLobby { get; private set; } = CSteamID.Nil;
        public string PendingInviteFrom { get; private set; } = string.Empty;

        public bool TryInitialize()
        {
            if (IsInitialized)
                return true;

            // Steam reads the App ID from these variables when no steam_appid.txt sits beside the executable.
            Environment.SetEnvironmentVariable("SteamAppId", STEAM_APP_ID);
            Environment.SetEnvironmentVariable("SteamGameId", STEAM_APP_ID);
            ESteamAPIInitResult result = SteamAPI.InitEx(out string error);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Status = $"Steam init failed ({result}): {error}";
                return false;
            }

            IsInitialized = true;
            _lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
            _lobbyInvite = Callback<LobbyInvite_t>.Create(OnLobbyInvite);
            _presenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnPresenceJoinRequested);
            _avatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);
            Status = $"Steam ready as {SteamFriends.GetPersonaName()}";
            return true;
        }

        public void Tick()
        {
            if (IsInitialized)
                SteamAPI.RunCallbacks();
        }

        public void CreateLobby()
        {
            _isHost = true;
            _lobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MAX_MEMBERS));
            Status = "Creating lobby...";
        }

        public void InviteFriends()
        {
            // The overlay is injected only into processes Steam can hook (usually not the Unity Editor).
            if (!SteamUtils.IsOverlayEnabled())
            {
                Status = "Steam overlay unavailable. Choose a friend below to send an invite.";
                return;
            }

            SteamFriends.ActivateGameOverlayInviteDialog(_lobby);
            Status = "Choose a friend below or in the Steam invite window.";
        }

        /// <summary>
        /// Online friends for in-game invites. The Steam overlay often does not attach to the Unity Editor,
        /// so the overlay invite dialog may never open there.
        /// </summary>
        public IReadOnlyList<SteamFriend> RefreshOnlineFriends()
        {
            _friends.Clear();
            int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < count; i++)
            {
                CSteamID friend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                if (SteamFriends.GetFriendPersonaState(friend) != EPersonaState.k_EPersonaStateOffline)
                    _friends.Add(new SteamFriend(friend, SteamFriends.GetFriendPersonaName(friend)));
            }

            return _friends;
        }

        /// <summary>Sends a Steam lobby invite directly; the friend's running game receives <c>LobbyInvite_t</c>.</summary>
        public void InviteFriend(SteamFriend friend)
        {
            bool sent = SteamMatchmaking.InviteUserToLobby(_lobby, friend.Id);
            Status = sent ? $"Invite sent to {friend.Name}" : $"Invite to {friend.Name} failed";
        }

        public void AcceptInvite()
        {
            CSteamID lobby = PendingInviteLobby;
            PendingInviteLobby = CSteamID.Nil;
            Join(lobby);
        }

        public bool TryJoinById(string lobbyId)
        {
            if (!ulong.TryParse(lobbyId.Trim(), out ulong id)) return false;
            Join(new CSteamID(id));
            return true;
        }

        public void Join(CSteamID lobby)
        {
            JoinStarting?.Invoke();
            _isHost = false;
            _requestedLobby = lobby;
            // Reuse an unfinished request for this lobby instead of issuing overlapping joins to it.
            if (!_lobbyJoins.ContainsKey(lobby))
            {
                var result = CallResult<LobbyEnter_t>.Create((entered, failed) => OnLobbyEntered(lobby, entered, failed));
                _lobbyJoins.Add(lobby, result);
                result.Set(SteamMatchmaking.JoinLobby(lobby));
            }
            Status = $"Joining lobby {lobby}...";
        }

        // Game started from a Steam invite: "+connect_lobby <lobbyId>".
        public bool TryJoinFromLaunchArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == CONNECT_LOBBY_ARG && ulong.TryParse(args[i + 1], out ulong lobbyId))
                {
                    Join(new CSteamID(lobbyId));
                    return true;
                }
            }

            return false;
        }

        /// <summary>Friends-list status, e.g. "In lobby 2/4" or "In battle".</summary>
        public void SetStatus(string status)
        {
            if (IsInitialized) SteamFriends.SetRichPresence(STATUS_PRESENCE, status);
        }

        /// <summary>
        /// The player's small Steam avatar; false while Steam still downloads it (<see cref="AvatarLoaded"/> follows)
        /// or without Steam.
        /// </summary>
        public bool TryGetAvatar(ulong steamId, out Texture2D avatar)
        {
            if (_avatars.TryGetValue(steamId, out avatar)) return true;
            avatar = null;
            if (!IsInitialized || steamId == 0UL) return false;
            int image = SteamFriends.GetSmallFriendAvatar(new CSteamID(steamId));
            // 0: no avatar set; -1: downloading, AvatarImageLoaded_t follows.
            if (image <= 0 || !SteamUtils.GetImageSize(image, out uint width, out uint height)) return false;
            byte[] rgba = new byte[width * height * 4];
            if (!SteamUtils.GetImageRGBA(image, rgba, rgba.Length)) return false;
            avatar = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
            // Steam rows run top-down; textures bottom-up.
            int row = (int)width * 4;
            byte[] flipped = new byte[rgba.Length];
            for (int y = 0; y < height; y++)
                Array.Copy(rgba, y * row, flipped, ((int)height - 1 - y) * row, row);
            avatar.LoadRawTextureData(flipped);
            avatar.Apply();
            _avatars.Add(steamId, avatar);
            return true;
        }

        public void ClearPresence()
        {
            if (IsInitialized) SteamFriends.ClearRichPresence();
        }

        public void Leave()
        {
            _isHost = false;
            _requestedLobby = CSteamID.Nil;
            if (!InLobby)
                return;

            SteamMatchmaking.LeaveLobby(_lobby);
            SteamFriends.ClearRichPresence();
            _lobby = CSteamID.Nil;
            Status = "Left lobby";
        }

        public void Shutdown()
        {
            if (!IsInitialized)
                return;

            Leave();
            foreach (CallResult<LobbyEnter_t> join in _lobbyJoins.Values) join.Dispose();
            _lobbyJoins.Clear();
            SteamAPI.Shutdown();
            IsInitialized = false;
        }

        private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
        {
            if (!_isHost)
            {
                if (!ioFailure && result.m_eResult == EResult.k_EResultOK)
                    SteamMatchmaking.LeaveLobby(new CSteamID(result.m_ulSteamIDLobby));
                return;
            }

            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Status = $"Lobby creation failed: {result.m_eResult}";
                Failed?.Invoke(OnlineError.ConnectionFailed);
                return;
            }

            _lobby = new CSteamID(result.m_ulSteamIDLobby);
            string hostId = SteamUser.GetSteamID().ToString();
            SteamMatchmaking.SetLobbyData(_lobby, GAME_DATA, GAME_KEY);
            SteamMatchmaking.SetLobbyData(_lobby, VERSION_DATA, Application.version);
            SteamMatchmaking.SetLobbyData(_lobby, HOST_DATA, hostId);
            SteamFriends.SetRichPresence(CONNECT_PRESENCE, $"{CONNECT_LOBBY_ARG} {_lobby.m_SteamID}");
            Status = $"Hosting lobby {_lobby.m_SteamID}";
            HostLobbyReady?.Invoke(hostId);
        }

        private void OnJoinRequested(GameLobbyJoinRequested_t request) => Join(request.m_steamIDLobby);

        // Friends list "Join Game" while the game runs: the friend's "connect" rich presence.
        private void OnPresenceJoinRequested(GameRichPresenceJoinRequested_t request)
        {
            string[] parts = request.m_rgchConnect.Split(' ');
            if (parts.Length == 2 && parts[0] == CONNECT_LOBBY_ARG && ulong.TryParse(parts[1], out ulong lobbyId))
                Join(new CSteamID(lobbyId));
        }

        private void OnAvatarLoaded(AvatarImageLoaded_t loaded)
        {
            if (loaded.m_iImage > 0) AvatarLoaded?.Invoke();
        }

        private void OnLobbyInvite(LobbyInvite_t invite)
        {
            if (_isHost) return;
            PendingInviteLobby = new CSteamID(invite.m_ulSteamIDLobby);
            PendingInviteFrom = SteamFriends.GetFriendPersonaName(new CSteamID(invite.m_ulSteamIDUser));
            Status = $"Invite from {PendingInviteFrom}";
            InviteReceived?.Invoke();
        }

        private void OnLobbyEntered(CSteamID requestedLobby, LobbyEnter_t entered, bool ioFailure)
        {
            _lobbyJoins[requestedLobby].Dispose();
            _lobbyJoins.Remove(requestedLobby);
            CSteamID lobby = new CSteamID(entered.m_ulSteamIDLobby);
            bool enteredLobby = !ioFailure &&
                entered.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess;
            if (requestedLobby != _requestedLobby)
            {
                // A cancelled/superseded operation may still join Steam; do not reopen its game session.
                if (enteredLobby && lobby != _lobby) SteamMatchmaking.LeaveLobby(lobby);
                return;
            }

            _requestedLobby = CSteamID.Nil;
            if (!enteredLobby)
            {
                Status = $"Lobby join failed: {(EChatRoomEnterResponse)entered.m_EChatRoomEnterResponse}";
                Failed?.Invoke(!ioFailure && entered.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseFull
                    ? OnlineError.LobbyFull
                    : OnlineError.ConnectionFailed);
                return;
            }

            string game = SteamMatchmaking.GetLobbyData(lobby, GAME_DATA);
            string version = SteamMatchmaking.GetLobbyData(lobby, VERSION_DATA);
            if (game != GAME_KEY || version != Application.version)
            {
                SteamMatchmaking.LeaveLobby(lobby);
                Status = $"Lobby mismatch: game '{game}', version '{version}' (local {Application.version})";
                Failed?.Invoke(OnlineError.VersionMismatch);
                return;
            }

            _lobby = lobby;
            // Friends of a member can join the same lobby from the friends list.
            SteamFriends.SetRichPresence(CONNECT_PRESENCE, $"{CONNECT_LOBBY_ARG} {lobby.m_SteamID}");
            Status = $"In lobby {lobby.m_SteamID} ({SteamMatchmaking.GetNumLobbyMembers(lobby)} members)";
            JoinedHost?.Invoke(SteamMatchmaking.GetLobbyData(lobby, HOST_DATA));
        }
    }
}
