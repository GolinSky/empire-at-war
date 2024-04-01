using System;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Models.Factions;
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
        [Inject] private IGameCommand GameCommand { get; }


        public override void Initialize()
        {
            base.Initialize();
            startGameButton.onClick.AddListener(OnStartGame);
            SetData(playerFactionDropdown);
            SetData(enemyFactionDropdown);
        }

        private void SetData(TMP_Dropdown dropdown)
        {
            dropdown.options.Clear();
            foreach (var factionType in Enum.GetNames(typeof(FactionType)))
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
                    GetEnum(playerFactionDropdown.captionText.text),
                    GetEnum(enemyFactionDropdown.captionText.text));
        }

        private FactionType GetEnum(string text) => Enum.Parse<FactionType>(text);

    }
}