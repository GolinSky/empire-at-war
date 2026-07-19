using EmpireAtWar.Ui.Base;
using EmpireAtWar.Entities.MenuUi;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class MainMenuInstaller : MonoInstaller
    {
        [SerializeField] private UiService uiService;

        public override void InstallBindings()
        {
            // Bind UI Service
            Container.BindInterfacesAndSelfTo<UiService>().FromInstance(uiService).AsSingle();
            Container
                .BindFactory<UiType, Transform, BaseUi, UiFacade>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<UiInstaller>();

            // Bind Main Menu MVP Components
            Container.BindInterfacesAndSelfTo<MenuUiModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<MenuUiController>().AsSingle();
            Container.BindInterfacesTo<MainMenuOrchestrator>().AsSingle();
        }
    }
}
