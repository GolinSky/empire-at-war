using EmpireAtWar.Commands;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MenuUi
{
    public class MenuUiView : View<IMenuUiModel, IMenuUiCommand>
    {
        [SerializeField] private Button startDemoButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitApplicationButton;
        
        protected override void OnInitialize()
        {
            startDemoButton.onClick.AddListener(Command.StartDemo);
            optionsButton.onClick.AddListener(Command.OpenOptions);
            quitApplicationButton.onClick.AddListener(Command.ExitApplication);
        }

        protected override void OnDispose()
        {
            startDemoButton.onClick.RemoveListener(Command.StartDemo);
            optionsButton.onClick.RemoveListener(Command.OpenOptions);
            quitApplicationButton.onClick.RemoveListener(Command.ExitApplication);
        }
    }
}