using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Entities.MainMenu;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Mvc;
using EmpireAtWar.Extentions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class MainMenuInstaller : MonoInstaller
    {
        [Inject] private IAssetService Repository { get; }

        public override void InstallBindings()
        {
            Container.BindScriptableObject<TooltipSettings>(Repository);
            Container.BindScriptableObject<TooltipIconData>(Repository);
            Container.Bind<TooltipTiming>().FromMethod(context =>
                context.Container.Resolve<TooltipSettings>().Timing).AsSingle();
            Container.Bind<ITooltipClock>().To<TooltipClock>().AsSingle();
            Container.BindInterfacesAndSelfTo<TooltipModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<TooltipService>().AsSingle().NonLazy();
            Container.BindInterfacesTo<TooltipUiController>().AsSingle().NonLazy();
            Container
                .BindInterfacesAndSelfTo<UiService>()
                .FromComponentInNewPrefab(Repository.LoadPrefab(nameof(UiService)))
                .AsSingle()
                .NonLazy();
            Container
                .BindFactory<UiType, Transform, BaseUi, UiFactory>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<UiInstaller>();
            Container.BindInterfacesAndSelfTo<UiCancelRouter>().AsSingle();

            // Bind Main Menu MVP Components
            Container.BindInterfacesAndSelfTo<MainMenuModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<MainRouteController>().AsSingle();
            Container.BindInterfacesTo<MainMenuOrchestrator>().AsSingle();
        }
    }
}
