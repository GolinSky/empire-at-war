using EmpireAtWar.Extentions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public abstract class DynamicEntityInstaller<TEntity, TModel> : MonoInstaller
        where TEntity : MonoBehaviour, IController
        where TModel : Model
    {
        protected TEntity Entity { get; private set; }
        protected Vector3 StartPosition { get; private set; }
        protected IRepository Repository { get; private set; }

        protected virtual Transform EntityTransformParent => transform;
        protected virtual string ModelPathPrefix { get; } = string.Empty;
        protected virtual string ModelPathPostfix { get; } = string.Empty;
        protected virtual string PrefabPathPrefix { get; } = string.Empty;
        protected virtual string PrefabPathPostfix { get; } = string.Empty;

        [Inject]
        public void Constructor(IRepository repository, Vector3 startPosition)
        {
            Repository = repository;
            StartPosition = startPosition;
        }

        public sealed override void InstallBindings()
        {
            BindData();
            BindModel();
            BindComponents();
            BindEntity();
            AssignEntity();
            Container.Install<MonoComponentInstaller>(new object[] { Entity.transform });
            OnEntityCreated();
        }

        protected virtual void OnBindData()
        {
        }

        protected virtual void BindComponents()
        {
        }

        protected virtual void OnModelCreated()
        {
        }

        protected virtual void OnEntityCreated()
        {
        }

        private void BindData()
        {
            Container.BindEntityExt(StartPosition);
            OnBindData();
        }

        private void BindModel()
        {
            ModelDependencyBuilder
                .ConstructBuilder(Container)
                .AppendToPath(ModelPathPrefix, ModelPathPostfix)
                .BindFromNewScriptable<TModel>(Repository, OnModelCreated);
        }

        private void BindEntity()
        {
            PrefabDependencyBuilder
                .ConstructBuilder(Container)
                .AppendToPath(PrefabPathPrefix, PrefabPathPostfix)
                .BindFromNewComponent<TEntity>(Repository, EntityTransformParent);
        }

        private void AssignEntity()
        {
            Entity = Container.Resolve<TEntity>();
        }
    }
}
