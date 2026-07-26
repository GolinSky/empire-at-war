using EmpireAtWar.Ui.Base;
using EmpireAtWar.Entities.MenuUi;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class MainMenuInstaller : MonoInstaller
    {
        [Inject] private IRepository Repository { get; }

        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<UiService>()
                .FromComponentInNewPrefab(Repository.LoadPrefab(nameof(UiService)))
                .AsSingle()
                .NonLazy();
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
