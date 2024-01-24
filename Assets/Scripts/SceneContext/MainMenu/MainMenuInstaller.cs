using EmpireAtWar;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.MenuUi;
using EmpireAtWar.Views.MenuUi;
using UnityEngine;
using Zenject;

public class MainMenuInstaller : MonoInstaller
{
    [SerializeField] private MenuUiView menuUiView;
    [SerializeField] private MenuUiModel menuUiModel;
    
    
    public override void InstallBindings()
    {
        Container.BindEntityNoCommand<MenuUiController, MenuUiView, MenuUiModel>(
            menuUiModel,
            menuUiView);
    }
}