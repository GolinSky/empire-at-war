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
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Fade;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Entities.MainMenu.ServerBrowser;
using EmpireAtWar.Entities.MainMenu.SinglePlayer;
using EmpireAtWar.Entities.MainMenu.Tutorial;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;
using SurvivalModel = EmpireAtWar.Entities.MainMenu.Survival.SurvivalModel;
using ILifecycleInitializable = EmpireAtWar.Lifecycle.IInitializable;
using ILifecycleLateDisposable = EmpireAtWar.Lifecycle.ILateDisposable;
using ILifecycleLateTickable = EmpireAtWar.Lifecycle.ILateTickable;

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
            builder.Register<SinglePlayerRouteUiController>(Lifetime.Singleton)
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
            builder.Register<SteamPopupService>(Lifetime.Singleton).As<ISteamPopupService>();
            builder.Register<OnlineRouteUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ServerBrowserRouteUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<TutorialSetupModel>(Lifetime.Singleton);
            builder.Register<TutorialSetupHintsController>(Lifetime.Singleton)
                .As<ILifecycleLateTickable, ILifecycleLateDisposable>().AsSelf();
            // The tutorial route reuses the Skirmish setup view; its presenter interface stays unregistered.
            builder.Register<TutorialRouteUiController>(Lifetime.Singleton)
                .As<ITutorialRoute, ILifecycleLateDisposable>();
            // Lobby screens reuse the setup views; their presenter interfaces stay unregistered.
            builder.Register<OnlineSkirmishLobbyUiController>(Lifetime.Singleton)
                .As<IOnlineSkirmishLobbyScreen, ILifecycleInitializable, ILifecycleLateDisposable>();
            builder.Register<OnlineSurvivalLobbyUiController>(Lifetime.Singleton)
                .As<IOnlineSurvivalLobbyScreen, ILifecycleInitializable, ILifecycleLateDisposable>();
            builder.Register<SettingsRouteUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
