using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Networking.Online.Messages;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    /// <summary>
    /// Online Skirmish lobby on the Skirmish setup screen. The host's screen is the truth: members are seated in
    /// rows, every change is published to the clients, and members' own row edits arrive as requests. A client
    /// renders the host's snapshot; its Start button toggles ready and Close leaves the lobby.
    /// </summary>
    public sealed class OnlineSkirmishLobbyUiController : UiController, ISkirmishPresenter, IOnlineSkirmishLobbyScreen,
        ISkirmishAvatarSource, IInitializable, ILateDisposable
    {
        private readonly OnlineMatchService _online;
        private readonly TeamColorPalette _palette;
        private readonly IMenuFactionSfxService _factionAudio;
        private readonly TooltipRequests _tooltips;
        private readonly ISteamPopupService _steamPopups;
        private TooltipHoverSubscription _tooltipHover;
        // The lobby keeps its own setup so the offline Skirmish screen is untouched.
        private readonly SkirmishModel _model = new SkirmishModel();
        private SkirmishUi _ui;
        private PopupHandle _invitePicker;
        private bool _isOpen;
        private string _appliedSetup = string.Empty;

        public OnlineSkirmishLobbyUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            OnlineMatchService online, TeamColorPalette palette, IMenuFactionSfxService factionAudio,
            ITooltipService tooltipService, ISteamPopupService steamPopups)
            : base(uiService, cancelRouter)
        {
            _tooltips = new TooltipRequests(tooltipService);
            _online = online;
            _palette = palette;
            _factionAudio = factionAudio;
            _steamPopups = steamPopups;
        }

        private bool IsHost => _online.Role == OnlineRole.Host;

        public void Initialize()
        {
            _online.Changed += Sync;
            _online.SlotEditRequested += ApplyMemberEdit;
            _model.Changed += OnModelChanged;
        }

        public void LateDispose()
        {
            _online.Changed -= Sync;
            _online.SlotEditRequested -= ApplyMemberEdit;
            _model.Changed -= OnModelChanged;
            if (_tooltipHover != null) _tooltipHover.Dispose();
            _tooltipHover = null;
            CloseInvitePicker();
            if (_ui != null)
            {
                _ui.InviteRequested -= InviteFriends;
                _ui.Dispose();
            }

            _ui = null;
            Unfocus();
        }

        public void Show()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SkirmishUi>();
                _ui.SetModel(_model);
                _ui.AssignPresenter(this);
                _ui.SetData(_palette);
                _ui.SetAvatarSource(this);
                _ui.Initialize();
                _ui.InviteRequested += InviteFriends;
                _tooltipHover = new TooltipHoverSubscription(_ui.TooltipHover, HandleTooltipHover, _tooltips);
            }

            _isOpen = true;
            _appliedSetup = string.Empty;
            if (IsHost) _model.BeginHostLobby(OnlineLobby.HOST_CONNECTION_ID, _online.Steam.PersonaName);
            else _model.BeginClientLobby();
            Sync();
            _ui.Show();
            Focus();
        }

        public void Hide()
        {
            _isOpen = false;
            _tooltips.HideAll();
            CloseInvitePicker();
            _ui.Hide();
            Unfocus();
        }

        // X and Escape leave the lobby; the online route then closes this screen.
        public void Close()
        {
            CloseInvitePicker();
            _online.Leave();
        }

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        public void StartGame()
        {
            if (!IsHost)
            {
                _online.SetReady(!_model.IsLocalReady);
                return;
            }

            if (_model.CanStart)
            {
                CloseInvitePicker();
                _online.StartSkirmish(_model.CreateBattle(), _model.CreateMemberSlots());
            }
        }

        /// <summary>Host only (clients cannot open the dropdown). A member's row offers Kick, which frees the seat.</summary>
        public void SelectSlotOccupant(int slotIndex, int occupantIndex)
        {
            SkirmishSlotSetup slot = _model.Slots[slotIndex];
            if (slot.IsHuman) _online.Kick(slot.MemberId);
            else _model.SelectSlotOccupant(slotIndex, (SkirmishSlotOccupant)occupantIndex);
        }

        public void SelectSlotFaction(int slotIndex, int factionIndex)
        {
            SkirmishSlotSetup slot = _model.Slots[slotIndex];
            if (_model.IsLocalSlot(slotIndex) && slot.Faction != (FactionType)factionIndex)
                _factionAudio.PlaySelection((FactionType)factionIndex);
            if (IsHost) _model.SelectSlotFaction(slotIndex, (FactionType)factionIndex);
            else _online.SendSlotEdit(factionIndex, slot.Team, slot.ColorIndex);
        }

        public void SelectSlotTeam(int slotIndex, int teamIndex)
        {
            SkirmishSlotSetup slot = _model.Slots[slotIndex];
            if (IsHost) _model.SelectSlotTeam(slotIndex, teamIndex);
            else _online.SendSlotEdit((int)slot.Faction, teamIndex, slot.ColorIndex);
        }

        public void SelectSlotColor(int slotIndex, int colorIndex)
        {
            SkirmishSlotSetup slot = _model.Slots[slotIndex];
            if (IsHost) _model.SelectSlotColor(slotIndex, colorIndex);
            else _online.SendSlotEdit((int)slot.Faction, slot.Team, colorIndex);
        }

        public void SelectPlanet(int index) => _model.SelectPlanet((PlanetType)index);

        public void SelectMapSize(int index) => _model.SelectMapSize((MapSize)index);

        public void SelectVictoryCondition(int index) => _model.SelectVictoryCondition((BattleVictoryCondition)index);

        public void SelectStartingMoney(float amount) => _model.SelectStartingMoney(amount);

        public void SelectSuperWeapons(bool enabled) => _model.SelectSuperWeapons(enabled);

        private void Sync()
        {
            if (!_isOpen) return;
            if (!_online.IsInLobby || _online.IsInBattle || _online.Mode != LobbyMode.Skirmish)
            {
                CloseInvitePicker();
                return;
            }
            if (IsHost) _model.SeatMembers(CreateSeats());
            else if (_online.SetupJson.Length > 0 && _online.SetupJson != _appliedSetup)
            {
                _appliedSetup = _online.SetupJson;
                _model.ApplyLobby(JsonUtility.FromJson<SkirmishLobbySnapshot>(_appliedSetup), _online.LocalConnectionId);
            }

            bool canInvite = IsHost && _online.IsSteamLobby;
            if (!canInvite) CloseInvitePicker();
            _ui.SetInviteAvailable(canInvite);
            _ui.ShowLobbyStatus(CreateStatus());
        }

        private void OnModelChanged()
        {
            if (!_isOpen) return;
            _ui.Render();
            if (IsHost) _online.PublishSetup(JsonUtility.ToJson(_model.CaptureLobby()));
        }

        private void ApplyMemberEdit(int connectionId, LobbySlotEditMessage edit) =>
            _model.TryEditMemberSlot(connectionId, (FactionType)edit.Faction, edit.Team, edit.Color);

        private void InviteFriends() => _invitePicker = _steamPopups.OpenInvitePicker();

        private void CloseInvitePicker()
        {
            _steamPopups.CloseInvitePicker(_invitePicker);
            _invitePicker = null;
        }

        public bool TryGetAvatar(ulong steamId, out Texture2D avatar) => _online.Steam.TryGetAvatar(steamId, out avatar);

        private List<SkirmishLobbySeat> CreateSeats() => _online.Members
            .Where(member => member.Connected)
            .Select(member => new SkirmishLobbySeat(member.ConnectionId, member.Name, member.Ready, member.SteamId))
            .ToList();

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isOpen, () => new TooltipContent(title: (string)key,
                description: (string)key switch
                {
                    "Start battle" => IsHost
                        ? _model.AllHumansReady ? "Start the battle for every player."
                            : "Start now, even though not every player is ready."
                        : _model.IsLocalReady ? "Tell the host you are not ready yet." : "Tell the host you are ready.",
                    "Close" => "Leave the lobby.",
                    "Planet" => $"The battlefield, picked by the host. Current: {_model.Planet}.",
                    "Map size" => $"The generated battlefield size, picked by the host. Current: {_model.MapSize}.",
                    "Victory condition" => $"The battle objective, picked by the host. Current: {_model.VictoryCondition}.",
                    "Starting credits" => $"Credits per player at battle start: {_model.StartingMoney:0}.",
                    "Player" => IsHost
                        ? "Players pick their own faction, team and colour. You set AI rows, open seats and can kick a player."
                        : "Pick your own faction, team and colour. The host sets the other rows.",
                    _ => throw new ArgumentOutOfRangeException(nameof(key))
                }));

        private string CreateStatus()
        {
            if (_online.LastError != OnlineError.None) return OnlineErrorText.Describe(_online.LastError);
            if (!IsHost) return "Pick your faction, team and colour, then press READY. The host starts the battle.";
            string addresses = string.Join(", ", _online.LocalAddresses().Select(address => $"{address}:{_online.Port}"));
            return _online.IsSteamLobby
                ? $"Friends join from a Steam invite or by IP {addresses}."
                : $"Friends join by IP {addresses}.";
        }
    }
}
