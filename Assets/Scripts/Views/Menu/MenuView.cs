using EmpireAtWar.Commands.Menu;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Menu
{
    public class MenuView : View<IMenuModelModelObserver, IMenuCommand>
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button openMenuButton;
        [SerializeField] private Canvas menuCanvas;
        
        protected override void OnInitialize()
        {
            openMenuButton.onClick.AddListener(Command.OpenMenu);
            openMenuButton.onClick.AddListener(EnableMenuCanvas);
            exitButton.onClick.AddListener(Command.ExitSkirmish);
            resumeButton.onClick.AddListener(Command.ResumeGame);
            resumeButton.onClick.AddListener(DisableMenuCanvas);
        }
        
        protected override void OnDispose()
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