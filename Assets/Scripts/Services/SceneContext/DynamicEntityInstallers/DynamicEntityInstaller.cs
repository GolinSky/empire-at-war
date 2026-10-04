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

        private GameObjectContext _context;

        private Vector3 _startPosition;

        protected IAssetService AssetService { get; private set; }

        protected virtual string DataPath => typeof(TData).Name;
        protected virtual string PrefabPath => typeof(TEntity).Name + VIEW_POSTFIX;

        [Inject]
        public void Constructor(IAssetService assetService, GameObjectContext context, Vector3 startPosition)
        {
            AssetService = assetService;
            _startPosition = startPosition;
            _context = context;
        }

        public sealed override void InstallBindings()
        {
            Container.BindEntityExt(_startPosition);

            TData data = AssetService.Load<TData>(DataPath);
            Container.BindInterfacesAndSelfTo<TData>().FromNewScriptableObject(data).AsSingle();

            InstallFeatures(data);

            TEntity entity = Container.InstantiatePrefabForInstall(
                AssetService.LoadComponent<TEntity>(PrefabPath), transform, _context);
            Container.BindInterfacesAndSelfTo<TEntity>().FromInstance(entity).AsSingle();
            Container.Bind<Transform>()
                .WithId(EntityBindType.ViewTransform)
                .FromResolveGetter<TEntity>(view => view.transform)
                .AsCached();

            Container.Install<EntityInstaller>(new object[] { entity });
        }

        protected abstract void InstallFeatures(TData data);
    }
}
