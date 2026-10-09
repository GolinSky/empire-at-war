using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Extensions
{
    public class ViewDependencyBuilder : DependencyBuilder<ViewDependencyBuilder>
    {
        private ViewDependencyBuilder(DiContainer container) : base(container)
        {
        }

        public void BindFromNewComponent<TView>(IAssetService assetService, Transform parent)
            where TView : IView
        {
            string pathToFile = ConstructName<TView>();

            Container
                .BindInterfacesAndSelfTo<TView>()
                .FromComponentInNewPrefab(assetService.Load<GameObject>(pathToFile))
                .UnderTransform(parent)
                .AsSingle();
        }

        public static ViewDependencyBuilder ConstructBuilder(DiContainer container)
        {
            return new ViewDependencyBuilder(container);
        }
    }
}