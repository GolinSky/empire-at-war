using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EmpireAtWar.Components.Ui.Tooltip;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public class SkirmishUi : BaseUi, ISkirmishUi, ITooltipHoverView
    {
        private ISkirmishModelObserver _model;
        private ISkirmishRouteNavigation _navigation;

        // Labels for the AI rows, in SkirmishSlotOccupant order (Closed .. AiUltraHard).
        private static readonly string[] AI_OCCUPANT_OPTIONS =
        {
            "Closed", "AI Easy", "AI Medium", "AI Hard", "AI Ultra Hard"
        };
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private SkirmishSlotRowView[] slotRows;
        [SerializeField] private TMP_Dropdown planetsDropdown;
        [SerializeField] private RawImage planetPreviewImage;
        [SerializeField] private Texture2D coruscantPreview;
        [SerializeField] private Texture2D kaminoPreview;
        [SerializeField] private TMP_Dropdown mapSizeDropdown;
        [SerializeField] private TMP_Dropdown victoryConditionDropdown;
        [SerializeField] private Slider startingMoneySlider;
        [SerializeField] private TMP_Text startingMoneyText;
        [SerializeField] private TMP_Text activeSlotsText;
        [SerializeField] private TMP_Text readinessText;
        [SerializeField] private TMP_Text matchSummaryText;
        [SerializeField] private Graphic readinessIndicator;
        [SerializeField] private CanvasGroup startGameContent;
        [SerializeField] private TooltipHoverView tooltipHover;
        private TeamColorPalette _palette;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_model == null || _navigation == null || _palette == null)
            {
                throw new InvalidOperationException("Skirmish UI dependencies must be set before initialization.");
            }

            string[] factionOptions = Enum.GetNames(typeof(FactionType));
            string[] teamOptions = CreateTeamOptions(_model.TeamCount);
            string[] colorOptions = CreateColorOptions(_palette);
            for (int i = 0; i < slotRows.Length; i++)
            {
                SkirmishSlotRowView row = slotRows[i];
                row.Initialize(i, _model.Slots[i].IsHuman, AI_OCCUPANT_OPTIONS, factionOptions, teamOptions, colorOptions);
                row.OccupantChanged += _navigation.SelectSlotOccupant;
                row.FactionChanged += _navigation.SelectSlotFaction;
                row.TeamChanged += _navigation.SelectSlotTeam;
                row.ColorChanged += _navigation.SelectSlotColor;
            }

            SetData<PlanetType>(planetsDropdown);
            SetData<MapSize>(mapSizeDropdown);
            SetData<BattleVictoryCondition>(victoryConditionDropdown);
            victoryConditionDropdown.options[(int)BattleVictoryCondition.DestroyEnemyFleet].text = "Destroy enemy fleet";
            victoryConditionDropdown.options[(int)BattleVictoryCondition.DestroyOpponentBase].text = "Destroy opponent base";
            victoryConditionDropdown.RefreshShownValue();
            SetStartingMoneySliderData();
            Render();

            _model.Changed += Render;
            closeButton.onClick.AddListener(_navigation.Close);
            startGameButton.onClick.AddListener(_navigation.StartGame);
            planetsDropdown.onValueChanged.AddListener(_navigation.SelectPlanet);
            mapSizeDropdown.onValueChanged.AddListener(_navigation.SelectMapSize);
            victoryConditionDropdown.onValueChanged.AddListener(_navigation.SelectVictoryCondition);
            startingMoneySlider.onValueChanged.AddListener(OnStartingMoneySliderChanged);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            _model.Changed -= Render;
            foreach (SkirmishSlotRowView row in slotRows)
            {
                row.OccupantChanged -= _navigation.SelectSlotOccupant;
                row.FactionChanged -= _navigation.SelectSlotFaction;
                row.TeamChanged -= _navigation.SelectSlotTeam;
                row.ColorChanged -= _navigation.SelectSlotColor;
                row.Dispose();
            }

            closeButton.onClick.RemoveListener(_navigation.Close);
            startGameButton.onClick.RemoveListener(_navigation.StartGame);
            planetsDropdown.onValueChanged.RemoveListener(_navigation.SelectPlanet);
            mapSizeDropdown.onValueChanged.RemoveListener(_navigation.SelectMapSize);
            victoryConditionDropdown.onValueChanged.RemoveListener(_navigation.SelectVictoryCondition);
            startingMoneySlider.onValueChanged.RemoveListener(OnStartingMoneySliderChanged);
            _isInitialized = false;
        }

        public void SetModel(ISkirmishModelObserver model)
        {
            _model = model;
        }

        public void SetNavigation(ISkirmishRouteNavigation navigation)
        {
            _navigation = navigation;
        }

        public void SetData(TeamColorPalette palette)
        {
            _palette = palette;
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
            _navigation.SelectStartingMoney(rawValue);
        }

        private void Render()
        {
            for (int i = 0; i < slotRows.Length; i++)
            {
                slotRows[i].Render(_model.Slots[i], _palette.GetColor(_model.Slots[i].ColorIndex));
            }

            // A match needs at least two opposing teams among the open rows.
            startGameButton.interactable = _model.CanStart;
            planetsDropdown.SetValueWithoutNotify((int)_model.Planet);
            planetPreviewImage.texture = _model.Planet switch
            {
                PlanetType.Coruscant => coruscantPreview,
                PlanetType.Kamino => kaminoPreview,
                _ => throw new ArgumentOutOfRangeException(nameof(_model.Planet), _model.Planet, null)
            };
            mapSizeDropdown.SetValueWithoutNotify((int)_model.MapSize);
            victoryConditionDropdown.SetValueWithoutNotify((int)_model.VictoryCondition);
            startingMoneySlider.SetValueWithoutNotify(_model.StartingMoney);
            startingMoneyText.text = $"${(int)_model.StartingMoney:N0}";
            activeSlotsText.text = $"{_model.ActivePlayerCount} / {slotRows.Length} SLOTS ACTIVE";
            readinessText.text = _model.CanStart ? "READY TO DEPLOY" : "OPPOSING TEAM REQUIRED";
            matchSummaryText.text = _model.CanStart
                ? $"{_model.ActivePlayerCount} players   /   {_model.ActiveTeamCount} opposing teams   /   ${_model.StartingMoney:N0} per player"
                : "Enable at least two opposing teams to start.";
            readinessIndicator.color = _model.CanStart ? new Color32(105, 199, 157, 255) : new Color32(245, 184, 76, 255);
            startGameContent.alpha = _model.CanStart ? 1f : 0.38f;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
