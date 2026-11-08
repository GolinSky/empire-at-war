using System.Collections.Generic;
using System.Linq;
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
        IInitializable, ILateDisposable
    {
        private readonly OnlineMatchService _online;
        private readonly TeamColorPalette _palette;
        private readonly IMenuFactionSfxService _factionAudio;
        // The lobby keeps its own setup so the offline Skirmish screen is untouched.
        private readonly SkirmishModel _model = new SkirmishModel();
        private SkirmishUi _ui;
        private bool _isOpen;
        private string _appliedSetup = string.Empty;

        public OnlineSkirmishLobbyUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            OnlineMatchService online, TeamColorPalette palette, IMenuFactionSfxService factionAudio)
            : base(uiService, cancelRouter)
        {
            _online = online;
            _palette = palette;
            _factionAudio = factionAudio;
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
                _ui.Initialize();
                _ui.InviteRequested += InviteFriends;
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
            _ui.Hide();
            Unfocus();
        }

        // X and Escape leave the lobby; the online route then closes this screen.
        public void Close() => _online.Leave();

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

            if (_model.CanStart) _online.StartSkirmish(_model.CreateBattle(), _model.CreateMemberSlots());
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
            if (!_isOpen || _online.Mode != LobbyMode.Skirmish) return;
            if (IsHost) _model.SeatMembers(CreateSeats());
            else if (_online.SetupJson.Length > 0 && _online.SetupJson != _appliedSetup)
            {
                _appliedSetup = _online.SetupJson;
                _model.ApplyLobby(JsonUtility.FromJson<SkirmishLobbySnapshot>(_appliedSetup), _online.LocalConnectionId);
            }

            _ui.SetInviteAvailable(IsHost && _online.IsSteamLobby);
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

        private void InviteFriends() => _online.Steam.InviteFriends();

        private List<SkirmishLobbySeat> CreateSeats() => _online.Members
            .Where(member => member.Connected)
            .Select(member => new SkirmishLobbySeat(member.ConnectionId, member.Name, member.Ready))
            .ToList();

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
