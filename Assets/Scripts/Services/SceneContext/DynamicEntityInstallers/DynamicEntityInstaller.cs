using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Extentions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public abstract class DynamicEntityInstaller<TEntity, TData> : MonoInstaller
        where TEntity : MonoBehaviour, IController
        where TData : Data
    {
        private const string VIEW_POSTFIX = "View";

        private Vector3 _startPosition;

        protected IAssetService Repository { get; private set; }

        protected virtual string DataPath => typeof(TData).Name;
        protected virtual string PrefabPath => typeof(TEntity).Name + VIEW_POSTFIX;

        [Inject]
        public void Constructor(IAssetService repository, Vector3 startPosition)
        {
            Repository = repository;
            _startPosition = startPosition;
        }

        public sealed override void InstallBindings()
        {
            Container.BindEntityExt(_startPosition);

            TData data = Repository.Load<TData>(DataPath);
            Container.BindInterfacesAndSelfTo<TData>().FromNewScriptableObject(data).AsSingle();

            InstallFeatures(data);

            Container.BindInterfacesAndSelfTo<TEntity>()
                .FromComponentInNewPrefab(Repository.Load<GameObject>(PrefabPath))
                .UnderTransform(transform)
                .AsSingle();
            Container.Bind<Transform>()
                .WithId(EntityBindType.ViewTransform)
                .FromResolveGetter<TEntity>(entity => entity.transform)
                .AsCached();

            // Spawned during install on purpose: FromComponentsInHierarchy bindings need the view in the hierarchy.
            TEntity entity = Container.Resolve<TEntity>();
            Container.Install<EntityInstaller>(new object[] { entity });
        }

        protected abstract void InstallFeatures(TData data);
    }
}
