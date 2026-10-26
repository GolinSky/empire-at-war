using EmpireAtWar.Entities.Fade;
using EmpireAtWar.Entities.MainMenu;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Play;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Fade;
using EmpireAtWar.Services.Audio;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;
using SurvivalModel = EmpireAtWar.Entities.MainMenu.Survival.SurvivalModel;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class MainMenuScope : AssetLifetimeScope
    {
        [SerializeField] private TeamColorPalette teamColorPalette;
        [SerializeField] private InterfaceSfxData interfaceSfxData;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            TooltipRegistrations.Register(builder, Assets);
            UiRegistrations.Register(builder, Assets, transform);
            builder.Register<FadeUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<FadeService>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<PlayRouteUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<LoadGameRouteUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<MainMenuUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MainMenuOrchestrator>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(teamColorPalette);
            builder.RegisterInstance(interfaceSfxData);
            builder.Register<MenuFactionSfxService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SkirmishModel>(Lifetime.Singleton);
            builder.RegisterInstance(Assets.Load<FactionCatalog>(nameof(FactionCatalog)));
            builder.Register<SimulationDraft>(Lifetime.Singleton);
            builder.Register<SimulationMenuLauncher>(Lifetime.Singleton).As<ISimulationLauncher>();
            builder.Register<SimulationSetupUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SurvivalModel>(Lifetime.Singleton);
            builder.Register<SurvivalRouteUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<SettingsModel>(Lifetime.Singleton);
            builder.Register<SettingsDraftEditor>(Lifetime.Singleton);
            builder.Register<KeyBindingEditor>(Lifetime.Singleton);
            builder.Register<SkirmishRouteUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SettingsRouteUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
