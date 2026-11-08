using System;
using Steamworks;
using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Minimal Steam lobby flow for the spike: init, friends-only lobby, overlay invite, join request, launch arg.
    // App ID 480 (Spacewar) shares its lobby namespace, so lobbies carry game + version data and joins check it.
    public sealed class SpikeSteamLobby
    {
        public const string GAME_KEY = "empire-at-war";
        private const string GAME_DATA = "game";
        private const string VERSION_DATA = "version";
        private const string HOST_DATA = "host";
        private const string CONNECT_LOBBY_ARG = "+connect_lobby";
        private const int MAX_MEMBERS = 4;

        private CallResult<LobbyCreated_t> _lobbyCreated;
        private Callback<LobbyEnter_t> _lobbyEntered;
        private Callback<GameLobbyJoinRequested_t> _joinRequested;
        private CSteamID _lobby = CSteamID.Nil;
        private bool _isHost;

        public event Action<string> HostLobbyReady;
        public event Action<string> JoinedHost;
        public event Action<string> Failed;

        public bool IsInitialized { get; private set; }
        public string Status { get; private set; } = "Steam not initialized";
        public bool InLobby => _lobby != CSteamID.Nil;

        public bool TryInitialize()
        {
            if (IsInitialized)
                return true;

            ESteamAPIInitResult result = SteamAPI.InitEx(out string error);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Status = $"Steam init failed ({result}): {error}";
                return false;
            }

            IsInitialized = true;
            _lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            _joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
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

        public void InviteFriends() => SteamFriends.ActivateGameOverlayInviteDialog(_lobby);

        public void Join(CSteamID lobby)
        {
            _isHost = false;
            SteamMatchmaking.JoinLobby(lobby);
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

        public void Leave()
        {
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
            SteamAPI.Shutdown();
            IsInitialized = false;
        }

        private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
        {
            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Failed?.Invoke($"Lobby creation failed: {result.m_eResult}");
                return;
            }

            _lobby = new CSteamID(result.m_ulSteamIDLobby);
            string hostId = SteamUser.GetSteamID().ToString();
            SteamMatchmaking.SetLobbyData(_lobby, GAME_DATA, GAME_KEY);
            SteamMatchmaking.SetLobbyData(_lobby, VERSION_DATA, Application.version);
            SteamMatchmaking.SetLobbyData(_lobby, HOST_DATA, hostId);
            SteamFriends.SetRichPresence("connect", $"{CONNECT_LOBBY_ARG} {_lobby.m_SteamID}");
            Status = $"Hosting lobby {_lobby.m_SteamID}";
            HostLobbyReady?.Invoke(hostId);
        }

        private void OnJoinRequested(GameLobbyJoinRequested_t request) => Join(request.m_steamIDLobby);

        private void OnLobbyEntered(LobbyEnter_t entered)
        {
            if (_isHost)
                return;

            CSteamID lobby = new CSteamID(entered.m_ulSteamIDLobby);
            string game = SteamMatchmaking.GetLobbyData(lobby, GAME_DATA);
            string version = SteamMatchmaking.GetLobbyData(lobby, VERSION_DATA);
            if (game != GAME_KEY || version != Application.version)
            {
                SteamMatchmaking.LeaveLobby(lobby);
                Failed?.Invoke($"Lobby mismatch: game '{game}', version '{version}' (local {Application.version})");
                return;
            }

            _lobby = lobby;
            Status = $"In lobby {lobby.m_SteamID} ({SteamMatchmaking.GetNumLobbyMembers(lobby)} members)";
            JoinedHost?.Invoke(SteamMatchmaking.GetLobbyData(lobby, HOST_DATA));
        }
    }
}
