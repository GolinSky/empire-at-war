using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Presenters.MiniMap;
using VContainer;
using VContainer.Internal;
using VContainer.Unity;

namespace EmpireAtWar.Entities.BaseEntity
{
    public static class EntityInstaller
    {
        public static void Register(IContainerBuilder builder, EntityView view, MarkType? miniMapMarkOverride)
        {
            builder.Register<Entity>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces()
                .WithParameter<IReadOnlyList<IEntityFacade>>(resolver =>
                    resolver.Resolve<ContainerLocal<IReadOnlyList<IEntityFacade>>>().Value)
                .WithParameter<IReadOnlyList<IPooledEntityEvents>>(resolver =>
                    resolver.Resolve<ContainerLocal<IReadOnlyList<IPooledEntityEvents>>>().Value)
                .WithParameter<IReadOnlyList<IHangarLaunchEvents>>(resolver =>
                    resolver.Resolve<ContainerLocal<IReadOnlyList<IHangarLaunchEvents>>>().Value);
            builder.Register<EntityTransformFacade>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterComponent(view.gameObject.AddComponent<ViewEntity>()).AsImplementedInterfaces().AsSelf();
            builder.Register<MiniMapUnitMarkerPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter(miniMapMarkOverride);
        }
    }
}
