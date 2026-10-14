using EmpireAtWar.Entities.MainMenu;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class MainMenuScope : AssetLifetimeScope
    {
        [SerializeField] private TeamColorPalette teamColorPalette;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            TooltipSettings tooltipSettings = Assets.Load<TooltipSettings>(nameof(TooltipSettings));
            builder.RegisterInstance(tooltipSettings).AsImplementedInterfaces().AsSelf();
            AssetRegistrations.RegisterShared<TooltipIconData>(builder, Assets);
            builder.RegisterInstance(tooltipSettings.Timing);
            builder.Register<TooltipClock>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<TooltipModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<TooltipService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<TooltipUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            UiService uiService = Object.Instantiate(Assets.LoadComponent<UiService>(nameof(UiService)), transform);
            builder.RegisterComponent(uiService).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(uiService.Placement);
            builder.Register<UiCancelRouter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EmpireAtWar.Entities.Fade.FadeUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<EmpireAtWar.Services.Fade.FadeService>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<EmpireAtWar.Entities.MainMenu.Play.PlayRouteUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<MainMenuUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MainMenuOrchestrator>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(teamColorPalette);
            builder.Register<SkirmishModel>(Lifetime.Singleton);
            builder.RegisterInstance(Assets.Load<FactionCatalog>(nameof(FactionCatalog)));
            builder.Register<SimulationDraft>(Lifetime.Singleton);
            builder.Register<SimulationMenuLauncher>(Lifetime.Singleton).As<ISimulationLauncher>();
            builder.Register<SimulationSetupUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EmpireAtWar.Entities.MainMenu.Survival.SurvivalModel>(Lifetime.Singleton);
            builder.Register<EmpireAtWar.Entities.MainMenu.Survival.SurvivalRouteUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<SettingsModel>(Lifetime.Singleton);
            builder.Register<SettingsDraftEditor>(Lifetime.Singleton);
            builder.Register<KeyBindingEditor>(Lifetime.Singleton);
            builder.Register<SkirmishRouteUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SettingsRouteUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterLifecycle();
        }
    }
}
