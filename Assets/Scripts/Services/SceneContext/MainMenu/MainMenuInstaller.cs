using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Entities.MainMenu;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Mvc;
using EmpireAtWar.Extensions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class MainMenuInstaller : MonoInstaller
    {
        [Inject] private IAssetService AssetService { get; }

        public override void InstallBindings()
        {
            TooltipSettings tooltipSettings = AssetService.Load<TooltipSettings>(nameof(TooltipSettings));
            Container.BindInterfacesAndSelfTo<TooltipSettings>().FromInstance(tooltipSettings).AsSingle();
            Container.BindScriptableObject<TooltipIconData>(AssetService);
            Container.Bind<TooltipTiming>().FromInstance(tooltipSettings.Timing).AsSingle();
            Container.Bind<ITooltipClock>().To<TooltipClock>().AsSingle();
            Container.BindInterfacesAndSelfTo<TooltipModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<TooltipService>().AsSingle().NonLazy();
            Container.BindInterfacesTo<TooltipUiController>().AsSingle().NonLazy();
            UiService uiService = Object.Instantiate(AssetService.LoadComponent<UiService>(nameof(UiService)));
            Container.BindInterfacesAndSelfTo<UiService>().FromInstance(uiService).AsSingle();
            Container.QueueForInject(uiService);
            Container.Bind<IUiPlacement>().FromInstance(uiService.Placement).AsSingle();
            Container.BindInterfacesAndSelfTo<UiCancelRouter>().AsSingle();
            Container.BindInterfacesAndSelfTo<EmpireAtWar.Entities.Fade.FadeUiController>().AsSingle();
            Container.BindInterfacesTo<EmpireAtWar.Services.Fade.FadeService>().AsSingle();

            Container.BindInterfacesAndSelfTo<MainMenuUiController>().AsSingle();
            Container.BindInterfacesTo<MainMenuOrchestrator>().AsSingle();
        }
    }
}
