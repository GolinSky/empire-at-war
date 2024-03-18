using EmpireAtWar;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.MenuUi;
using EmpireAtWar.Views.MenuUi;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

public class MainMenuInstaller : MonoInstaller
{
    [SerializeField] private MenuUiView menuUiView;
    
    [Inject]
    private IRepository Repository { get; }
    
    public override void InstallBindings()
    {
        Container
            .BindModel<MenuUiModel>(Repository)
            .BindInterfaces<MenuUiController>()
            .BindViewFromInstance(menuUiView);
    }
}