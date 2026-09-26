using System;
using EmpireAtWar.Entities.MenuUi.Popups;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
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
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_Dropdown playerFactionDropdown;
        [SerializeField] private TMP_Dropdown enemyFactionDropdown;
        [SerializeField] private TMP_Dropdown planetsDropdown;
        [SerializeField] private TMP_Dropdown victoryConditionDropdown;
        [SerializeField] private TMP_Dropdown enemyDifficultyDropdown;
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

            SetData<FactionType>(playerFactionDropdown);
            SetData<FactionType>(enemyFactionDropdown);
            SetData<PlanetType>(planetsDropdown);
            SetData<BattleVictoryCondition>(victoryConditionDropdown);
            SetData<EnemyAiDifficulty>(enemyDifficultyDropdown);
            SetStartingMoneySliderData();
            Render();

            _model.Changed += Render;
            closeButton.onClick.AddListener(_presenter.CloseSkirmish);
            startGameButton.onClick.AddListener(_presenter.StartGame);
            playerFactionDropdown.onValueChanged.AddListener(_presenter.SelectPlayerFaction);
            enemyFactionDropdown.onValueChanged.AddListener(_presenter.SelectEnemyFaction);
            planetsDropdown.onValueChanged.AddListener(_presenter.SelectPlanet);
            victoryConditionDropdown.onValueChanged.AddListener(_presenter.SelectVictoryCondition);
            enemyDifficultyDropdown.onValueChanged.AddListener(_presenter.SelectEnemyDifficulty);
            startingMoneySlider.onValueChanged.AddListener(OnStartingMoneySliderChanged);
            _isInitialized = true;
        }

        private void SetData<TEnum>(TMP_Dropdown dropdown)
        {
            dropdown.options.Clear();
            foreach (var factionType in Enum.GetNames(typeof(TEnum)))
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(factionType));
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
            playerFactionDropdown.SetValueWithoutNotify((int)_model.PlayerFaction);
            enemyFactionDropdown.SetValueWithoutNotify((int)_model.EnemyFaction);
            planetsDropdown.SetValueWithoutNotify((int)_model.Planet);
            victoryConditionDropdown.SetValueWithoutNotify((int)_model.VictoryCondition);
            enemyDifficultyDropdown.SetValueWithoutNotify((int)_model.EnemyDifficulty);
            startingMoneySlider.SetValueWithoutNotify(_model.StartingMoney);
            playerFactionDropdown.RefreshShownValue();
            enemyFactionDropdown.RefreshShownValue();
            startingMoneyText.text = $"${(int)_model.StartingMoney:N0}";
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            _model.Changed -= Render;
            closeButton.onClick.RemoveListener(_presenter.CloseSkirmish);
            startGameButton.onClick.RemoveListener(_presenter.StartGame);
            playerFactionDropdown.onValueChanged.RemoveListener(_presenter.SelectPlayerFaction);
            enemyFactionDropdown.onValueChanged.RemoveListener(_presenter.SelectEnemyFaction);
            planetsDropdown.onValueChanged.RemoveListener(_presenter.SelectPlanet);
            victoryConditionDropdown.onValueChanged.RemoveListener(_presenter.SelectVictoryCondition);
            enemyDifficultyDropdown.onValueChanged.RemoveListener(_presenter.SelectEnemyDifficulty);
            startingMoneySlider.onValueChanged.RemoveListener(OnStartingMoneySliderChanged);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
