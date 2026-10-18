using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace EmpireAtWar
{
    public abstract class DynamicEntityInstaller<TEntity, TData> : IInstaller
        where TEntity : EntityView
        where TData : Data
    {
        protected TEntity View { get; }
        protected TData Data { get; }
        protected EntityBindings Bindings => View.Bindings;
        protected ILocalPlayer LocalPlayer { get; }
        protected PlayerId Owner { get; }
        private readonly long _entityId;
        private readonly Vector3 _startPosition;

        protected DynamicEntityInstaller(TEntity view, TData data, ILocalPlayer localPlayer,
            PlayerId owner, long entityId, Vector3 startPosition)
        {
            View = view;
            Data = data;
            LocalPlayer = localPlayer;
            Owner = owner;
            _entityId = entityId;
            _startPosition = startPosition;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(Owner);
            builder.RegisterInstance(_entityId);
            builder.RegisterInstance(_startPosition);
            builder.RegisterInstance(View.transform).Keyed(EntityBindType.ViewTransform);
            builder.RegisterComponent(View).AsSelf().AsImplementedInterfaces();
            InstallFeatures(builder);
            EntityInstaller.Register(builder, View);
            foreach (MonoBehaviour injectable in Bindings.Injectables)
                builder.RegisterComponent(injectable).Keyed(injectable);
        }

        protected abstract void InstallFeatures(IContainerBuilder builder);
    }
}
