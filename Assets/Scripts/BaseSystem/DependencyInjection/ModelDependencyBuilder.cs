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

        /// <summary>Binds the loaded asset itself: read-only data is shared, never copied per context.</summary>
        public DiContainer BindShared<TModel>(IAssetService assetService)
            where TModel : Data
        {
            Container
                .BindInterfacesAndSelfTo<TModel>()
                .FromInstance(assetService.Load<TModel>(ConstructName<TModel>()))
                .AsSingle();
            return Container;
        }

        /// <summary>Binds a copy of the asset, for data that holds runtime state.</summary>
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
