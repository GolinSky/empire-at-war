using EmpireAtWar.LightWeightFramework.PopupCommands;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Ui.Popups
{
    public class SkirmishPopupUi : PopupUi
    {
        [SerializeField] private Button startGameButton;

        [Inject] private ISkirmishSetupCommand SkirmishSetupCommand { get; }


        public override void Initialize()
        {
            startGameButton.onClick.AddListener(OnStartGame);
        }
        
        public override void LateDispose()
        {
            startGameButton.onClick.RemoveListener(OnStartGame);
        }

        private void OnStartGame()
        {
            Debug.Log("OnStartGame");
            SkirmishSetupCommand.StartGame();
        }
    }
}