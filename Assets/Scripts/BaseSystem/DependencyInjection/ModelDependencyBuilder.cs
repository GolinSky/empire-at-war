using System;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Extensions
{
    //todo: remove 
    public class ModelDependencyBuilder:DependencyBuilder<ModelDependencyBuilder>
    {
        private ModelDependencyBuilder(DiContainer container) : base(container)
        {
        }

        public DiContainer BindFromNewScriptable<TModel>(IAssetService assetService,  Action onCompleted = null)
            where TModel : Data
        {
            string pathToFile = ConstructName<TModel>();
            
            Container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(assetService.Load<TModel>(pathToFile))
                .AsSingle()
                .OnInstantiated((context, o) =>
                {
                    onCompleted?.Invoke();
                });
            return Container;
        }

        public DiContainer BindFromNewScriptable<TModel>(IAssetService assetService, object id, Action onCompleted = null)
            where TModel : Data
        {
            string pathToFile = ConstructName<TModel>();
            
            Container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(assetService.Load<TModel>(pathToFile))
                .AsSingle()
                .WithConcreteId(id)
                .OnInstantiated((context, o) =>
                {
                    onCompleted?.Invoke();
                });
            return Container;
        }

        public static ModelDependencyBuilder ConstructBuilder(DiContainer container)
        {
            return new ModelDependencyBuilder(container);
        }
    }
}
