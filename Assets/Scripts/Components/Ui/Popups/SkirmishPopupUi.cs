using System;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Ui.Popups
{
    public class SkirmishPopupUi : PopupUi
    {
        private const float MIN_STARTING_MONEY = 500f;
        private const float MAX_STARTING_MONEY = 10000f;
        private const float DEFAULT_STARTING_MONEY = 2000f;
        private const float MONEY_STEP = 500f;
        private const FactionType DEFAULT_PLAYER_FACTION = FactionType.Republic;
        private const FactionType DEFAULT_ENEMY_FACTION = FactionType.Separatist;

        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_Dropdown playerFactionDropdown;
        [SerializeField] private TMP_Dropdown enemyFactionDropdown;
        [SerializeField] private TMP_Dropdown planetsDropdown;
        [SerializeField] private TMP_Dropdown victoryConditionDropdown;
        [SerializeField] private TMP_Dropdown enemyDifficultyDropdown;
        [SerializeField] private Slider startingMoneySlider;
        [SerializeField] private TMP_Text startingMoneyText;

        [Inject] private IGameCommand GameCommand { get; }

        public override void Initialize()
        {
            base.Initialize();
            if (startingMoneySlider == null)
            {
                throw new InvalidOperationException($"{nameof(startingMoneySlider)} is not assigned in {nameof(SkirmishPopupUi)}.");
            }

            startGameButton.onClick.AddListener(OnStartGame);
            SetData<FactionType>(playerFactionDropdown);
            SetData<FactionType>(enemyFactionDropdown);
            playerFactionDropdown.value = (int)DEFAULT_PLAYER_FACTION;
            enemyFactionDropdown.value = (int)DEFAULT_ENEMY_FACTION;
            playerFactionDropdown.RefreshShownValue();
            enemyFactionDropdown.RefreshShownValue();
            playerFactionDropdown.onValueChanged.AddListener(OnPlayerFactionChanged);
            enemyFactionDropdown.onValueChanged.AddListener(OnEnemyFactionChanged);
            SetData<PlanetType>(planetsDropdown);
            SetData<BattleVictoryCondition>(victoryConditionDropdown);
            SetData<EnemyAiDifficulty>(enemyDifficultyDropdown);
            SetStartingMoneySliderData();
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
            startingMoneySlider.minValue = MIN_STARTING_MONEY;
            startingMoneySlider.maxValue = MAX_STARTING_MONEY;
            startingMoneySlider.value = DEFAULT_STARTING_MONEY;
            startingMoneySlider.onValueChanged.AddListener(OnStartingMoneySliderChanged);
            UpdateStartingMoneyText(startingMoneySlider.value);
        }

        private void OnStartingMoneySliderChanged(float rawValue)
        {
            float snappedValue = Mathf.Round(rawValue / MONEY_STEP) * MONEY_STEP;
            if (Mathf.Abs(startingMoneySlider.value - snappedValue) > 0.01f)
            {
                startingMoneySlider.value = snappedValue;
            }

            UpdateStartingMoneyText(snappedValue);
        }

        private void UpdateStartingMoneyText(float amount)
        {
            if (startingMoneyText != null)
            {
                startingMoneyText.text = $"${(int)amount:N0}";
            }
        }

        public override void LateDispose()
        {
            base.LateDispose();
            startGameButton.onClick.RemoveListener(OnStartGame);
            playerFactionDropdown.onValueChanged.RemoveListener(OnPlayerFactionChanged);
            enemyFactionDropdown.onValueChanged.RemoveListener(OnEnemyFactionChanged);
            if (startingMoneySlider != null)
            {
                startingMoneySlider.onValueChanged.RemoveListener(OnStartingMoneySliderChanged);
            }
        }

        private void OnStartGame()
        {
            KeepFactionsDifferent(playerFactionDropdown, enemyFactionDropdown);
            GameCommand
                .StartGame(
                    GetEnum<FactionType>(playerFactionDropdown.captionText.text),
                    GetEnum<FactionType>(enemyFactionDropdown.captionText.text),
                    GetEnum<PlanetType>(planetsDropdown.captionText.text),
                    GetEnum<BattleVictoryCondition>(victoryConditionDropdown.captionText.text),
                    GetEnum<EnemyAiDifficulty>(enemyDifficultyDropdown.captionText.text),
                    startingMoneySlider.value);
        }

        private void OnPlayerFactionChanged(int _)
        {
            KeepFactionsDifferent(playerFactionDropdown, enemyFactionDropdown);
        }

        private void OnEnemyFactionChanged(int _)
        {
            KeepFactionsDifferent(enemyFactionDropdown, playerFactionDropdown);
        }

        private static void KeepFactionsDifferent(
            TMP_Dropdown changedDropdown,
            TMP_Dropdown otherDropdown)
        {
            if (changedDropdown.value != otherDropdown.value)
            {
                return;
            }

            if (otherDropdown.options.Count < 2)
            {
                throw new InvalidOperationException(
                    "Skirmish setup requires at least two factions.");
            }

            otherDropdown.value =
                (changedDropdown.value + 1) % otherDropdown.options.Count;
            otherDropdown.RefreshShownValue();
        }

        private TEnum GetEnum<TEnum>(string text) where TEnum : struct => Enum.Parse<TEnum>(text);
    }
}
