using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using global::VContainer;

namespace EmpireAtWar.Services.Tutorial.Guided
{
    /// <summary>
    /// The local player's side of the guided tutorial: the hint engine with the tutorial's own track, progress that
    /// always starts fresh, hints shown regardless of the setting, and the director.
    /// </summary>
    public static class GuidedTutorialRegistrations
    {
        public static void Register(IContainerBuilder builder, IAssetService assets)
        {
            TutorialScript script = assets.Load<TutorialScript>(nameof(TutorialScript));
            builder.RegisterInstance(script);
            builder.RegisterInstance(script.Track);
            builder.Register<AlwaysOnHintVisibility>(Lifetime.Singleton).As<IHintVisibility>();
            builder.Register<TutorialHintProgress>(Lifetime.Singleton).As<IHintProgressRepository>().AsSelf();
            HintRegistrations.RegisterEngine(builder);
            builder.Register<TutorialDirector>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
