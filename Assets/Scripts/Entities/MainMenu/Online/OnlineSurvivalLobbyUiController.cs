using System;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    /// <summary>
    /// Online Survival co-op lobby on the Survival setup screen. The host picks every setting and starts; each
    /// member joins as a co-op commander of the host's faction. Clients see the host's settings and toggle ready.
    /// </summary>
    public sealed class OnlineSurvivalLobbyUiController : UiController, ISurvivalPresenter, IOnlineSurvivalLobbyScreen,
        IInitializable, ILateDisposable
    {
        private const string START_LABEL = "START GAME";
        private const string FORCE_START_LABEL = "FORCE START";
        private const string READY_LABEL = "READY";
        private const string CANCEL_READY_LABEL = "NOT READY";
        // Co-op stations plus the mining ring do not fit on a Small map (SurvivalPreset rejects it).
        private static readonly MapSize[] COOP_MAP_SIZES = { MapSize.Medium, MapSize.Large };

        private readonly OnlineMatchService _online;
        private readonly ISteamPopupService _steamPopups;
        private readonly IConfirmPopupService _confirmPopups;
        // The lobby keeps its own setup so the offline Survival screen is untouched.
        private readonly SurvivalModel _model = new SurvivalModel();
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;
        private SurvivalUi _ui;
        private PopupHandle _invitePicker;
        private PopupHandle _leavePopup;
        private bool _isOpen;
        private string _appliedSetup = string.Empty;

        public OnlineSurvivalLobbyUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            OnlineMatchService online, ISteamPopupService steamPopups, IConfirmPopupService confirmPopups, ITooltipService tooltipService)
            : base(uiService, cancelRouter)
        {
            _confirmPopups = confirmPopups;
            _online = online;
            _steamPopups = steamPopups;
            _tooltips = new TooltipRequests(tooltipService);
        }

        private bool IsHost => _online.Role == OnlineRole.Host;

        public void Initialize()
        {
            _model.SelectMapSize(MapSize.Medium);
            _online.Changed += Sync;
            _model.Changed += OnModelChanged;
        }

        public void LateDispose()
        {
            if (_tooltipHover != null) _tooltipHover.Dispose();
            _online.Changed -= Sync;
            _model.Changed -= OnModelChanged;
            CloseInvitePicker();
            if (_ui != null)
            {
                _ui.InviteRequested -= InviteFriends;
                _ui.LobbyModeSwitch.ModeSelected -= SwitchMode;
                _ui.Dispose();
            }

            _ui = null;
            Unfocus();
        }

        public void Show()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SurvivalUi>();
                _ui.SetModel(_model);
                _ui.SetMapSizes(COOP_MAP_SIZES);
                _ui.AssignPresenter(this);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(_ui.TooltipHover, HandleTooltipHover, _tooltips);
                _ui.InviteRequested += InviteFriends;
                _ui.LobbyModeSwitch.ModeSelected += SwitchMode;
            }

            _isOpen = true;
            _appliedSetup = string.Empty;
            if (IsHost) OnModelChanged();
            Sync();
            _ui.Show();
            Focus();
        }

        public void Hide()
        {
            _isOpen = false;
            _tooltips.HideAll();
            if (_leavePopup != null) _confirmPopups.Close(_leavePopup);
            CloseInvitePicker();
            _ui.Hide();
            Unfocus();
        }

        // X and Escape ask first; confirming leaves the lobby and the online route then closes this screen.
        public void Close()
        {
            if (_leavePopup != null) return;
            _tooltips.HideAll();
            _leavePopup = _confirmPopups.ShowAccept(
                new AcceptPopupContent("Leave lobby?", "Leave the lobby and return to the Online screen?", "Leave", "Stay"),
                result =>
                {
                    _leavePopup = null;
                    if (result == ConfirmPopupResult.Accepted) Leave();
                });
        }

        private void Leave()
        {
            CloseInvitePicker();
            _online.LeaveLobby();
        }

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        public void StartBattle()
        {
            if (IsHost)
            {
                if (_online.TryStartSurvival(_model.CreateBattle())) CloseInvitePicker();
            }
            else _online.SetReady(!IsLocalReady());
        }

        // Settings are the host's; clients cannot open these controls.
        public void SelectPlayer(int index) => _model.SelectPlayer(FactionAt(index));
        public void SelectAlly(int index) => _model.SelectAlly(index == 0 ? null : FactionAt(index - 1));
        public void SelectEnemy(int index) => _model.SelectEnemy(FactionAt(index));
        public void SelectMapSize(MapSize size) => _model.SelectMapSize(size);
        public void SelectDifficulty(int index) => _model.SelectDifficulty((EnemyAiDifficulty)index);
        public void SelectSuperWeapons(bool enabled) => _model.SelectSuperWeapons(enabled);

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isOpen && _leavePopup == null, () => new TooltipContent(
                title: (string)key == "Start battle" && !IsHost ? "Ready" : (string)key,
                description: (string)key switch
                {
                    "Start battle" when !IsHost => IsLocalReady()
                        ? "Mark yourself as not ready." : "Tell the host you are ready.",
                    "Start battle" => _online.AllReady
                        ? "Start Survival for every commander." : "Start Survival before every commander is ready.",
                    "Close" => "Confirm that you want to leave the lobby.",
                    "Player faction" => "The host chooses the faction every commander uses.",
                    "Ally faction" => "The host can add an AI ally to help defend.",
                    "Enemy faction" => "The host chooses which faction sends the enemy waves.",
                    "Map size" => $"The host chooses the battlefield size. Current size is {_model.MapSize}.",
                    "Difficulty" => "The host sets how challenging the enemy waves are.",
                    _ => throw new ArgumentOutOfRangeException(nameof(key))
                }));

        private void Sync()
        {
            if (!_isOpen) return;
            if (!_online.IsInLobby || _online.IsInBattle || _online.Mode != LobbyMode.Survival)
            {
                CloseInvitePicker();
                _ui.SetInviteAvailable(false);
                return;
            }
            if (!IsHost && _online.SetupJson.Length > 0 && _online.SetupJson != _appliedSetup)
            {
                _appliedSetup = _online.SetupJson;
                _model.ApplyLobby(JsonUtility.FromJson<SurvivalLobbySnapshot>(_appliedSetup));
            }

            bool canInvite = _online.IsSteamLobby;
            if (!canInvite) CloseInvitePicker();
            _ui.SetInviteAvailable(canInvite);
            if (IsHost) _ui.LobbyModeSwitch.Render(_online.Mode, _online.CanSwitchLobbyMode(LobbyMode.Survival));
            else _ui.LobbyModeSwitch.Hide();
            RenderLobby();
        }

        private void OnModelChanged()
        {
            if (!_isOpen) return;
            _ui.Render();
            if (IsHost) _online.PublishSetup(JsonUtility.ToJson(_model.CaptureLobby()));
        }

        private void RenderLobby()
        {
            int ready = _online.Members.Count(member => member.Ready);
            int count = _online.Members.Count;
            string startText = IsHost ? _online.AllReady ? START_LABEL : FORCE_START_LABEL
                : IsLocalReady() ? CANCEL_READY_LABEL : READY_LABEL;
            List<SurvivalLobbyMemberState> members = _online.Members.Select(CreateMemberState).ToList();
            string status = _online.LastError != OnlineError.None ? OnlineErrorText.Describe(_online.LastError)
                : IsHost ? "Every member defends as a co-op commander of your faction."
                : "The host picks the settings. Press READY when set."
                    + (_online.IsSteamLobby ? " Invite friends from Steam." : string.Empty);
            _ui.RenderLobby(IsHost, startText, IsHost || _online.IsInLobby,
                $"{ready} / {count} COMMANDERS READY", status, members);
        }

        private SurvivalLobbyMemberState CreateMemberState(LobbyMember member)
        {
            bool isHost = member.ConnectionId == OnlineLobby.HOST_CONNECTION_ID;
            _online.Steam.TryGetAvatar(member.SteamId, out Texture2D avatar);
            return new SurvivalLobbyMemberState(member.Name,
                isHost ? "HOST" : member.Ready ? "READY" : "NOT READY", isHost || member.Ready, avatar);
        }

        private bool IsLocalReady() =>
            _online.Members.Any(member => member.ConnectionId == _online.LocalConnectionId && member.Ready);

        // The online route swaps this screen for the new mode's screen.
        private void SwitchMode(LobbyMode mode) => _online.SwitchLobbyMode(mode);

        private void InviteFriends() => _invitePicker = _steamPopups.OpenInvitePicker();

        private void CloseInvitePicker()
        {
            _steamPopups.CloseInvitePicker(_invitePicker);
            _invitePicker = null;
        }

        private static FactionType FactionAt(int index) =>
            (FactionType)Enum.GetValues(typeof(FactionType)).GetValue(index);
    }
}
