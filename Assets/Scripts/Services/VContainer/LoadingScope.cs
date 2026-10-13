using EmpireAtWar.Entities.Loading;
using EmpireAtWar.Lifecycle;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class LoadingScope : AssetLifetimeScope
    {
        [SerializeField] private LoadingUi view;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(view).AsImplementedInterfaces().AsSelf();
            builder.Register<LoadingUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SceneActivationFlow>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterLifecycle();
        }
    }
}
