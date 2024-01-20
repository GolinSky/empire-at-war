using EmpireAtWar.Commands;
using EmpireAtWar.Models.MenuUi;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.MenuUi
{
    public class MenuUiView : View<IMenuUiModel, IMenuUiCommand>
    {
        [SerializeField] private Button startDemoButton;
        [SerializeField] private Button quitApplicationButton;
        
        protected override void OnInitialize()
        {
            startDemoButton.onClick.AddListener(Command.StartDemo);
            quitApplicationButton.onClick.AddListener(Command.ExitApplication);
        }

        protected override void OnDispose()
        {
            startDemoButton.onClick.RemoveListener(Command.StartDemo);
            quitApplicationButton.onClick.RemoveListener(Command.ExitApplication);
        }
    }
}