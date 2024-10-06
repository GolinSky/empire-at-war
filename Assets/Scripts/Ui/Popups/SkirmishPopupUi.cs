using System;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Planet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Ui.Popups
{
    public class SkirmishPopupUi : PopupUi
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_Dropdown playerFactionDropdown;
        [SerializeField] private TMP_Dropdown enemyFactionDropdown;
        [SerializeField] private TMP_Dropdown planetsDropdown;
        [Inject] private IGameCommand GameCommand { get; }


        public override void Initialize()
        {
            base.Initialize();
            startGameButton.onClick.AddListener(OnStartGame);
            SetData<FactionType>(playerFactionDropdown);
            SetData<FactionType>(enemyFactionDropdown);
            SetData<PlanetType>(planetsDropdown);
        }

        private void SetData<TEnum>(TMP_Dropdown dropdown)
        {
            dropdown.options.Clear();
            foreach (var factionType in Enum.GetNames(typeof(TEnum)))
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(factionType));
            }
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
                    GetEnum<PlanetType>(planetsDropdown.captionText.text));
        }

        private TEnum GetEnum<TEnum>(string text) where TEnum : struct => Enum.Parse<TEnum>(text);

    }
}