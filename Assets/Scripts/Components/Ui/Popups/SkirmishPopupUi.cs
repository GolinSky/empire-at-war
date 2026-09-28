using System;
using EmpireAtWar.Entities.MenuUi.Popups;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Ui.Popups
{
    public class SkirmishPopupUi : BaseUi, ISkirmishPopupUi
    {
        // Labels for the AI rows, in SkirmishSlotOccupant order (Closed .. AiUltraHard).
        private static readonly string[] AI_OCCUPANT_OPTIONS =
        {
            "Closed", "AI Easy", "AI Medium", "AI Hard", "AI Ultra Hard"
        };

        [SerializeField] private Button closeButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private SkirmishSlotRowView[] slotRows;
        [SerializeField] private TMP_Dropdown planetsDropdown;
        [SerializeField] private TMP_Dropdown mapSizeDropdown;
        [SerializeField] private TMP_Dropdown victoryConditionDropdown;
        [SerializeField] private Slider startingMoneySlider;
        [SerializeField] private TMP_Text startingMoneyText;

        private ISkirmishPopupModelObserver _model;
        private ISkirmishPopupPresenter _presenter;
        private bool _isInitialized;

        public void SetModel(ISkirmishPopupModelObserver model)
        {
            _model = model;
        }

        public void SetPresenter(ISkirmishPopupPresenter presenter)
        {
            _presenter = presenter;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_model == null || _presenter == null)
            {
                throw new InvalidOperationException("Skirmish popup dependencies must be set before initialization.");
            }

            string[] factionOptions = Enum.GetNames(typeof(FactionType));
            string[] teamOptions = CreateTeamOptions(_model.TeamCount);
            for (int i = 0; i < slotRows.Length; i++)
            {
                SkirmishSlotRowView row = slotRows[i];
                row.Initialize(i, _model.Slots[i].IsHuman, AI_OCCUPANT_OPTIONS, factionOptions, teamOptions);
                row.OccupantChanged += _presenter.SelectSlotOccupant;
                row.FactionChanged += _presenter.SelectSlotFaction;
                row.TeamChanged += _presenter.SelectSlotTeam;
            }

            SetData<PlanetType>(planetsDropdown);
            SetData<MapSize>(mapSizeDropdown);
            SetData<BattleVictoryCondition>(victoryConditionDropdown);
            SetStartingMoneySliderData();
            Render();

            _model.Changed += Render;
            closeButton.onClick.AddListener(_presenter.CloseSkirmish);
            startGameButton.onClick.AddListener(_presenter.StartGame);
            planetsDropdown.onValueChanged.AddListener(_presenter.SelectPlanet);
            mapSizeDropdown.onValueChanged.AddListener(_presenter.SelectMapSize);
            victoryConditionDropdown.onValueChanged.AddListener(_presenter.SelectVictoryCondition);
            startingMoneySlider.onValueChanged.AddListener(OnStartingMoneySliderChanged);
            _isInitialized = true;
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

        private void Render()
        {
            for (int i = 0; i < slotRows.Length; i++)
            {
                slotRows[i].Render(_model.Slots[i]);
            }

            // A match needs at least two opposing teams among the open rows.
            startGameButton.interactable = _model.CanStart;
            planetsDropdown.SetValueWithoutNotify((int)_model.Planet);
            mapSizeDropdown.SetValueWithoutNotify((int)_model.MapSize);
            victoryConditionDropdown.SetValueWithoutNotify((int)_model.VictoryCondition);
            startingMoneySlider.SetValueWithoutNotify(_model.StartingMoney);
            startingMoneyText.text = $"${(int)_model.StartingMoney:N0}";
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
                row.OccupantChanged -= _presenter.SelectSlotOccupant;
                row.FactionChanged -= _presenter.SelectSlotFaction;
                row.TeamChanged -= _presenter.SelectSlotTeam;
                row.Dispose();
            }

            closeButton.onClick.RemoveListener(_presenter.CloseSkirmish);
            startGameButton.onClick.RemoveListener(_presenter.StartGame);
            planetsDropdown.onValueChanged.RemoveListener(_presenter.SelectPlanet);
            mapSizeDropdown.onValueChanged.RemoveListener(_presenter.SelectMapSize);
            victoryConditionDropdown.onValueChanged.RemoveListener(_presenter.SelectVictoryCondition);
            startingMoneySlider.onValueChanged.RemoveListener(OnStartingMoneySliderChanged);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
