using System;
using EmpireAtWar.LightWeightFramework.PopupCommands;
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
        [Inject] private ISkirmishSetupCommand SkirmishSetupCommand { get; }


        public override void Initialize()
        {
            startGameButton.onClick.AddListener(OnStartGame);
            playerFactionDropdown.options.Clear();
            foreach (var factionType in Enum.GetNames(typeof(FactionType)))
            {
                playerFactionDropdown.options.Add(new TMP_Dropdown.OptionData(factionType));
            }
        }
        
        public override void LateDispose()
        {
            startGameButton.onClick.RemoveListener(OnStartGame);
        }

        private void OnStartGame()
        {
            Debug.Log("OnStartGame");
            SkirmishSetupCommand.StartGame(Enum.Parse<FactionType>(playerFactionDropdown.captionText.text));
        }
    }
}