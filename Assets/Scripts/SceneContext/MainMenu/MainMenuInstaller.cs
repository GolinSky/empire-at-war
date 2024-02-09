using EmpireAtWar;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.MenuUi;
using EmpireAtWar.Views.MenuUi;
using UnityEngine;
using Zenject;

public class MainMenuInstaller : MonoInstaller
{
    [SerializeField] private MenuUiView menuUiView;
    
    
    public override void InstallBindings()
    {
        Container.BindEntity<MenuUiController, MenuUiView, MenuUiModel>(
            menuUiView);
    }
}