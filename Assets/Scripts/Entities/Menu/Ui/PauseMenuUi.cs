using EmpireAtWar.Commands.Menu;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views.Menu
{
    public interface IPauseMenuUiView
    {
        void SetMenuVisible(bool isVisible);
    }

    public class PauseMenuUi : BaseUi<IMenuModelModelObserver, IMenuCommand>, IPauseMenuUiView, IInitializable, ILateDisposable
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject menuPanel;

        public void Initialize()
        {
            exitButton.onClick.AddListener(Command.ExitSkirmish);
            resumeButton.onClick.AddListener(Command.ResumeGame);
        }

        public void LateDispose()
        {
            exitButton.onClick.RemoveListener(Command.ExitSkirmish);
            resumeButton.onClick.RemoveListener(Command.ResumeGame);
        }

        public void SetMenuVisible(bool isVisible)
        {
            menuPanel.SetActive(isVisible);
        }
    }
}
