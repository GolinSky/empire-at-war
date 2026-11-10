using EmpireAtWar.Entities.TutorialUi;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Tutorial;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>In-match hints for the local human player. Hint progress lives in the project scope.</summary>
    public static class HintRegistrations
    {
        /// <param name="hasCheatPanel">The economy module registers the cheat panel; its Tutorial tab restarts hints.</param>
        public static void Register(IContainerBuilder builder, IAssetService assets, bool hasCheatPanel)
        {
            if (hasCheatPanel) builder.Register<HintCheatPresenter>(Lifetime.Singleton).AsImplementedInterfaces();
            AssetRegistrations.RegisterShared<HintTrack>(builder, assets);
            builder.Register<SettingsHintVisibility>(Lifetime.Singleton).As<IHintVisibility>();
            RegisterEngine(builder);
        }

        /// <summary>
        /// The hint engine and card without a track, visibility or progress; the caller registers those.
        /// </summary>
        public static void RegisterEngine(IContainerBuilder builder)
        {
            builder.Register<HintConditionFactory>(Lifetime.Singleton);
            builder.Register<ScoutUnits>(Lifetime.Singleton);
            builder.Register<HintStates>(Lifetime.Singleton);
            builder.Register<HintTargets>(Lifetime.Singleton);
            builder.Register<LocalUnitDamageTracker>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HintService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HintUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
