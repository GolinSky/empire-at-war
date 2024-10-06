using LightWeightFramework.Components;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public class ViewDependencyBuilder : DependencyBuilder<ViewDependencyBuilder>
    {
        private ViewDependencyBuilder(DiContainer container) : base(container)
        {
        }

        public void BindFromNewComponent<TView>(IRepository repository, Transform parent)
            where TView : IView
        {
            ConstructName<TView>();

            Container
                .BindInterfacesAndSelfTo<TView>()
                .FromComponentInNewPrefab(repository.Load<GameObject>(PathToFile))
                .UnderTransform(parent)
                .AsSingle();
        }

        public static ViewDependencyBuilder ConstructBuilder(DiContainer container)
        {
            return new ViewDependencyBuilder(container);
        }
    }
}