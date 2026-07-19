using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public class PrefabDependencyBuilder : DependencyBuilder<PrefabDependencyBuilder>
    {
        private PrefabDependencyBuilder(DiContainer container) : base(container)
        {
        }

        public void BindFromNewComponent<TEntity>(IRepository repository, Transform parent)
            where TEntity : Component
        {
            ConstructName<TEntity>();

            Container
                .BindInterfacesAndSelfTo<TEntity>()
                .FromComponentInNewPrefab(repository.Load<GameObject>(PathToFile))
                .UnderTransform(parent)
                .AsSingle();
        }

        public static PrefabDependencyBuilder ConstructBuilder(DiContainer container)
        {
            return new PrefabDependencyBuilder(container);
        }
    }
}
