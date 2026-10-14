using EmpireAtWar.Presenters.MiniMap;
using VContainer;
using VContainer.Unity;

namespace EmpireAtWar.Entities.BaseEntity
{
    public static class EntityInstaller
    {
        public static void Register(IContainerBuilder builder, EntityView view)
        {
            builder.Register<Entity>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<EntityTransformFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterComponent(view.gameObject.AddComponent<ViewEntity>()).AsImplementedInterfaces().AsSelf();
            builder.Register<MiniMapUnitMarkerPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
