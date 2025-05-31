using EmpireAtWar.Commands.Menu;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views.Menu
{
    public class MenuUi : BaseUi<IMenuModelModelObserver, IMenuCommand>, IInitializable, ILateDisposable
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button openMenuButton;
        [SerializeField] private Canvas menuCanvas;


        public void Initialize()
        {
            openMenuButton.onClick.AddListener(Command.OpenMenu);
            openMenuButton.onClick.AddListener(EnableMenuCanvas);
            exitButton.onClick.AddListener(Command.ExitSkirmish);
            resumeButton.onClick.AddListener(Command.ResumeGame);
            resumeButton.onClick.AddListener(DisableMenuCanvas);
        }

        public void LateDispose()
        {
            openMenuButton.onClick.RemoveListener(Command.OpenMenu);
            openMenuButton.onClick.AddListener(EnableMenuCanvas);
            exitButton.onClick.RemoveListener(Command.ExitSkirmish);
            resumeButton.onClick.RemoveListener(Command.ResumeGame);
            resumeButton.onClick.RemoveListener(DisableMenuCanvas);
        }
        
        private void EnableMenuCanvas()
        {
            SetMenuCanvasState(true);
        }
        
        private void DisableMenuCanvas()
        {
            SetMenuCanvasState(false);
        }

        private void SetMenuCanvasState(bool isActive)
        {
            menuCanvas.enabled = isActive;
        }

  
    }
}