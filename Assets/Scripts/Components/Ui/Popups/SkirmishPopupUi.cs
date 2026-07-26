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
        private static readonly int[] STARTING_MONEY_OPTIONS = { 500, 1000, 2000, 5000, 10000 };

        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_Dropdown playerFactionDropdown;
        [SerializeField] private TMP_Dropdown enemyFactionDropdown;
        [SerializeField] private TMP_Dropdown planetsDropdown;
        [SerializeField] private TMP_Dropdown victoryConditionDropdown;
        [SerializeField] private TMP_Dropdown enemyDifficultyDropdown;
        [SerializeField] private TMP_Dropdown startingMoneyDropdown;
        [Inject] private IGameCommand GameCommand { get; }


        public override void Initialize()
        {
            base.Initialize();
            startGameButton.onClick.AddListener(OnStartGame);
            SetData<FactionType>(playerFactionDropdown);
            SetData<FactionType>(enemyFactionDropdown);
            SetData<PlanetType>(planetsDropdown);
            SetData<BattleVictoryCondition>(victoryConditionDropdown);
            SetData<EnemyAiDifficulty>(enemyDifficultyDropdown);
            SetStartingMoneyData();
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

        private void SetStartingMoneyData()
        {
            startingMoneyDropdown.options.Clear();
            foreach (int amount in STARTING_MONEY_OPTIONS)
            {
                startingMoneyDropdown.options.Add(new TMP_Dropdown.OptionData(amount.ToString()));
            }

            startingMoneyDropdown.value = 1;
            startingMoneyDropdown.RefreshShownValue();
        }

        public override void LateDispose()
        {
            base.LateDispose();
            startGameButton.onClick.RemoveListener(OnStartGame);
        }

        private void OnStartGame()
        {
            GameCommand
                .StartGame(
                    GetEnum<FactionType>(playerFactionDropdown.captionText.text),
                    GetEnum<FactionType>(enemyFactionDropdown.captionText.text),
                    GetEnum<PlanetType>(planetsDropdown.captionText.text),
                    GetEnum<BattleVictoryCondition>(victoryConditionDropdown.captionText.text),
                    GetEnum<EnemyAiDifficulty>(enemyDifficultyDropdown.captionText.text),
                    STARTING_MONEY_OPTIONS[startingMoneyDropdown.value]);
        }

        private TEnum GetEnum<TEnum>(string text) where TEnum : struct => Enum.Parse<TEnum>(text);

    }
}
