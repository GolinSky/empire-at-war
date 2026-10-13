using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Lifecycle;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class PlanetScope : AssetLifetimeScope
    {
        [SerializeField] private PlanetView view;
        [SerializeField] private PlanetSuperWeaponOrigin superWeaponOrigin;

        public PlanetView View => view;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            AssetRegistrations.RegisterShared<PlanetData>(builder, Assets);
            builder.Register<PlanetController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(view).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(superWeaponOrigin);
            builder.RegisterLifecycle();
        }
    }
}
