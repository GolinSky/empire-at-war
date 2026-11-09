using EmpireAtWar.Entities.TutorialUi;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Tutorial;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>In-match hints for the local human player. Hint progress lives in the project scope.</summary>
    public static class HintRegistrations
    {
        public static void Register(IContainerBuilder builder, IAssetService assets)
        {
            AssetRegistrations.RegisterShared<HintTrack>(builder, assets);
            builder.Register<HintConditionFactory>(Lifetime.Singleton);
            builder.Register<LocalUnitDamageTracker>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HintService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<HintUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
