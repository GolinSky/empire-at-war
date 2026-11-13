using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EmpireAtWar.Components.Ui.Tooltip;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public class SkirmishUi : BaseUi
    {
        private ISkirmishModelObserver _model;
        private ISkirmishPresenter _presenter;

        private const string HUMAN_OCCUPANT_LABEL = "Human";
        private const string KICK_OPTION_LABEL = "Kick";
        private const string FORCE_START_LABEL = "FORCE START";
        private const string READY_LABEL = "READY";
        private const string CANCEL_READY_LABEL = "NOT READY";
        // Online AI rows list the open seat after the AI levels.
        private const int VACANT_OPTION_INDEX = 5;

        // Labels for the AI rows, in SkirmishSlotOccupant order (Closed .. AiUltraHard).
        private static readonly string[] AI_OCCUPANT_OPTIONS =
        {
            "Closed", "AI Easy", "AI Medium", "AI Hard", "AI Ultra Hard"
        };
        private static readonly string[] ONLINE_AI_OCCUPANT_OPTIONS =
        {
            "Closed", "AI Easy", "AI Medium", "AI Hard", "AI Ultra Hard", "Open"
        };
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_Text startGameLabel;
        [SerializeField] private Button inviteButton;
        [SerializeField] private LobbyModeSwitchView lobbyModeSwitch;
        [SerializeField] private SkirmishSlotRowView[] slotRows;
        [SerializeField] private Button planetButton;
        [SerializeField] private TMP_Text planetLabel;
        [SerializeField] private Button planetPreviewButton;
        [SerializeField] private RawImage planetPreviewImage;
        [SerializeField] private Texture2D coruscantPreview;
        [SerializeField] private Texture2D kaminoPreview;
        [SerializeField] private DictionaryWrapper<PlanetType, Texture2D> planetPreviews;
        [SerializeField] private TMP_Dropdown mapSizeDropdown;
        [SerializeField] private TMP_Dropdown victoryConditionDropdown;
        [SerializeField] private Slider startingMoneySlider;
        [SerializeField] private Toggle superWeaponsToggle;
        [SerializeField] private TMP_Text startingMoneyText;
        [SerializeField] private TMP_Text activeSlotsText;
        [SerializeField] private TMP_Text readinessText;
        [SerializeField] private TMP_Text matchSummaryText;
        [SerializeField] private Graphic readinessIndicator;
        [SerializeField] private CanvasGroup startGameContent;
        [SerializeField] private TooltipHoverView tooltipHover;
        private TeamColorPalette _palette;
        private string _offlineStartLabel;
        private string _lobbyStatus = string.Empty;
        private ISkirmishAvatarSource _avatars;

        private bool _isInitialized;
        private Button _planetOpener;

        public event Action PlanetSelectionRequested;

        /// <summary>Online lobby route only.</summary>
        public event Action InviteRequested;


        public TooltipHoverView TooltipHover => tooltipHover;
        /// <summary>Online lobby route only.</summary>
        public LobbyModeSwitchView LobbyModeSwitch => lobbyModeSwitch;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_model == null || _presenter == null || _palette == null)
            {
                throw new InvalidOperationException("Skirmish UI dependencies must be set before initialization.");
            }

            string[] factionOptions = Enum.GetNames(typeof(FactionType));
            string[] teamOptions = CreateTeamOptions(_model.TeamCount);
            string[] colorOptions = CreateColorOptions(_palette);
            for (int i = 0; i < slotRows.Length; i++)
            {
                SkirmishSlotRowView row = slotRows[i];
                row.Initialize(i, factionOptions, teamOptions, colorOptions);
                row.OccupantChanged += HandleOccupantChanged;
                row.FactionChanged += _presenter.SelectSlotFaction;
                row.TeamChanged += _presenter.SelectSlotTeam;
                row.ColorChanged += _presenter.SelectSlotColor;
            }

            SetData<MapSize>(mapSizeDropdown);
            SetData<BattleVictoryCondition>(victoryConditionDropdown);
            victoryConditionDropdown.options[(int)BattleVictoryCondition.DestroyEnemyFleet].text = "Destroy enemy fleet";
            victoryConditionDropdown.options[(int)BattleVictoryCondition.DestroyOpponentBase].text = "Destroy opponent base";
            victoryConditionDropdown.RefreshShownValue();
            SetStartingMoneySliderData();
            _offlineStartLabel = startGameLabel.text;
            SetInviteAvailable(false);
            lobbyModeSwitch.Initialize();
            Render();
            closeButton.onClick.AddListener(_presenter.Close);
            startGameButton.onClick.AddListener(_presenter.StartGame);
            inviteButton.onClick.AddListener(RaiseInviteRequested);
            planetButton.onClick.AddListener(OpenPlanetSelection);
            planetPreviewButton.onClick.AddListener(OpenPlanetPreviewSelection);
            mapSizeDropdown.onValueChanged.AddListener(_presenter.SelectMapSize);
            victoryConditionDropdown.onValueChanged.AddListener(_presenter.SelectVictoryCondition);
            startingMoneySlider.onValueChanged.AddListener(OnStartingMoneySliderChanged);
            superWeaponsToggle.onValueChanged.AddListener(_presenter.SelectSuperWeapons);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }
            foreach (SkirmishSlotRowView row in slotRows)
            {
                row.OccupantChanged -= HandleOccupantChanged;
                row.FactionChanged -= _presenter.SelectSlotFaction;
                row.TeamChanged -= _presenter.SelectSlotTeam;
                row.ColorChanged -= _presenter.SelectSlotColor;
                row.Dispose();
            }

            closeButton.onClick.RemoveListener(_presenter.Close);
            startGameButton.onClick.RemoveListener(_presenter.StartGame);
            inviteButton.onClick.RemoveListener(RaiseInviteRequested);
            lobbyModeSwitch.Dispose();
            planetButton.onClick.RemoveListener(OpenPlanetSelection);
            planetPreviewButton.onClick.RemoveListener(OpenPlanetPreviewSelection);
            mapSizeDropdown.onValueChanged.RemoveListener(_presenter.SelectMapSize);
            victoryConditionDropdown.onValueChanged.RemoveListener(_presenter.SelectVictoryCondition);
            startingMoneySlider.onValueChanged.RemoveListener(OnStartingMoneySliderChanged);
            superWeaponsToggle.onValueChanged.RemoveListener(_presenter.SelectSuperWeapons);
            _isInitialized = false;
        }

        public void SetModel(ISkirmishModelObserver model)
        {
            _model = model;
        }

        public void AssignPresenter(ISkirmishPresenter presenter)
        {
            if (_presenter != null)
            {
                throw new System.InvalidOperationException("Presenter is already assigned.");
            }
            _presenter = presenter;
        }

        public void SetData(TeamColorPalette palette)
        {
            _palette = palette;
        }

        /// <summary>Online lobby: Steam avatars for the members' rows.</summary>
        public void SetAvatarSource(ISkirmishAvatarSource avatars) => _avatars = avatars;

        /// <summary>Online lobby: the Steam invite button replaces the active slot count.</summary>
        public void SetInviteAvailable(bool available)
        {
            inviteButton.gameObject.SetActive(available);
            activeSlotsText.gameObject.SetActive(!available);
        }

        /// <summary>Online lobby: how friends reach this lobby, or the last error; replaces the match summary.</summary>
        public void ShowLobbyStatus(string status)
        {
            _lobbyStatus = status;
            if (_isInitialized) Render();
        }

        private static string[] CreateTeamOptions(int teamCount)
        {
            string[] options = new string[teamCount];
            for (int i = 0; i < teamCount; i++)
            {
                options[i] = $"Team {i + 1}";
            }

            return options;
        }

        // A colored mark supplies the swatch without depending on a symbol font.
        private static string[] CreateColorOptions(TeamColorPalette palette)
        {
            string[] options = new string[palette.Count];
            for (int i = 0; i < palette.Count; i++)
            {
                string hex = ColorUtility.ToHtmlStringRGB(palette.GetColor(i));
                options[i] = $"<mark=#{hex}FF padding=\"0,0,-20,-20\">  </mark>  {palette.GetName(i)}";
            }

            return options;
        }

        private void SetData<TEnum>(TMP_Dropdown dropdown)
        {
            dropdown.options.Clear();
            foreach (var optionName in Enum.GetNames(typeof(TEnum)))
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(optionName));
            }

            dropdown.value = 0;
            dropdown.RefreshShownValue();
        }

        private void SetStartingMoneySliderData()
        {
            startingMoneySlider.minValue = _model.MinStartingMoney;
            startingMoneySlider.maxValue = _model.MaxStartingMoney;
        }

        private void OnStartingMoneySliderChanged(float rawValue)
        {
            _presenter.SelectStartingMoney(rawValue);
        }

        public void Render()
        {
            for (int i = 0; i < slotRows.Length; i++)
            {
                slotRows[i].Render(_model.Slots[i], _palette.GetColor(_model.Slots[i].ColorIndex), CreateRowState(i));
            }

            bool canEditSettings = _model.CanEditSettings;
            planetButton.interactable = canEditSettings;
            planetPreviewButton.interactable = canEditSettings;
            mapSizeDropdown.interactable = canEditSettings;
            victoryConditionDropdown.interactable = canEditSettings;
            startingMoneySlider.interactable = canEditSettings;
            superWeaponsToggle.interactable = canEditSettings;
            planetLabel.text = PlanetSelectionPopupUi.GetPlanetName(_model.Planet);
            planetPreviewImage.texture = GetPlanetPreview(_model.Planet);
            mapSizeDropdown.SetValueWithoutNotify((int)_model.MapSize);
            victoryConditionDropdown.SetValueWithoutNotify((int)_model.VictoryCondition);
            startingMoneySlider.SetValueWithoutNotify(_model.StartingMoney);
            superWeaponsToggle.SetIsOnWithoutNotify(_model.SuperWeapons);
            startingMoneyText.text = $"${(int)_model.StartingMoney:N0}";
            activeSlotsText.text = $"{_model.ActivePlayerCount} / {slotRows.Length} SLOTS ACTIVE";
            // A match needs at least two opposing teams among the open rows.
            bool ready = _model.CanStart && (!_model.IsOnline || _model.AllHumansReady);
            readinessText.text = !_model.CanStart ? "OPPOSING TEAM REQUIRED"
                : !_model.IsOnline ? "READY TO DEPLOY"
                : _model.AllHumansReady ? "ALL COMMANDERS READY"
                : $"{_model.ReadyHumanCount} / {_model.HumanCount} COMMANDERS READY";
            matchSummaryText.text = _model.IsOnline && _lobbyStatus.Length > 0 ? _lobbyStatus
                : _model.CanStart
                    ? $"{_model.ActivePlayerCount} players   /   {_model.ActiveTeamCount} opposing teams   /   ${_model.StartingMoney:N0} per player"
                    : "Enable at least two opposing teams to start.";
            readinessIndicator.color = ready ? new Color32(109, 184, 226, 255) : new Color32(210, 169, 94, 255);
            RenderStartButton();
        }

        // Offline and host: start (forced while a member is not ready). Lobby client: toggles its ready tick.
        private void RenderStartButton()
        {
            bool isClient = _model.IsOnline && !_model.IsHost;
            bool interactable = isClient ? _model.LocalMemberId != SkirmishModel.NO_MEMBER : _model.CanStart;
            startGameButton.interactable = interactable;
            startGameLabel.text = isClient ? _model.IsLocalReady ? CANCEL_READY_LABEL : READY_LABEL
                : _model.IsOnline && !_model.AllHumansReady ? FORCE_START_LABEL
                : _offlineStartLabel;
            startGameContent.alpha = interactable ? 1f : 0.38f;
        }

        private SkirmishSlotRowState CreateRowState(int slotIndex)
        {
            SkirmishSlotSetup slot = _model.Slots[slotIndex];
            bool occupantEditable = _model.CanEditOccupant(slotIndex);
            bool detailsEditable = _model.CanEditDetails(slotIndex);
            bool teamEditable = _model.CanEditTeam(slotIndex);
            if (slot.IsHuman)
            {
                string name = _model.IsOnline ? slot.PlayerName : HUMAN_OCCUPANT_LABEL;
                string title = !_model.IsOnline ? "COMMANDER"
                    : $"{slot.PlayerName.ToUpperInvariant()}   /   {(slot.Ready ? "READY" : "NOT READY")}";
                string[] options = occupantEditable ? new[] { name, KICK_OPTION_LABEL } : new[] { name };
                Texture2D avatar = null;
                if (_model.IsOnline && slot.SteamId != 0UL) _avatars.TryGetAvatar(slot.SteamId, out avatar);
                return new SkirmishSlotRowState(title, _model.IsLocalSlot(slotIndex) ? "YOU" : "HUMAN", options, 0,
                    occupantEditable, detailsEditable, teamEditable, avatar);
            }

            bool isVacant = slot.Occupant == SkirmishSlotOccupant.Vacant;
            return new SkirmishSlotRowState(
                isVacant ? "OPEN SEAT   /   WAITING FOR A PLAYER" : $"PLAYER {slotIndex + 1}",
                isVacant ? "OPEN" : slot.IsOpen ? "AI" : "OFF",
                _model.IsOnline ? ONLINE_AI_OCCUPANT_OPTIONS : AI_OCCUPANT_OPTIONS,
                isVacant ? VACANT_OPTION_INDEX : (int)slot.Occupant,
                occupantEditable, detailsEditable, teamEditable, null);
        }

        // A member's row offers only Kick, which frees the seat; AI rows map option index to occupant.
        private void HandleOccupantChanged(int slotIndex, int optionIndex)
        {
            SkirmishSlotOccupant occupant = _model.Slots[slotIndex].IsHuman || optionIndex == VACANT_OPTION_INDEX
                ? SkirmishSlotOccupant.Vacant
                : (SkirmishSlotOccupant)optionIndex;
            _presenter.SelectSlotOccupant(slotIndex, (int)occupant);
        }

        private void RaiseInviteRequested() => InviteRequested?.Invoke();

        public Texture2D GetPlanetPreview(PlanetType planet) => planet switch
        {
            PlanetType.Coruscant => coruscantPreview,
            PlanetType.Kamino => kaminoPreview,
            _ => planetPreviews.Dictionary[planet]
        };

        public void RestorePlanetFocus()
        {
            if (IsVisible && _model.CanEditSettings) _planetOpener.Select();
        }

        private void OpenPlanetSelection()
        {
            _planetOpener = planetButton;
            PlanetSelectionRequested();
        }

        private void OpenPlanetPreviewSelection()
        {
            _planetOpener = planetPreviewButton;
            PlanetSelectionRequested();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
