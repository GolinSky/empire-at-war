using System;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Extentions
{
    //todo: remove 
    public class ModelDependencyBuilder:DependencyBuilder<ModelDependencyBuilder>
    {
        private ModelDependencyBuilder(DiContainer container) : base(container)
        {
        }

        public DiContainer BindFromNewScriptable<TModel>(IRepository repository,  Action onCompleted = null) 
            where TModel : Model
        {
            ConstructName<TModel>();
            
            Container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(repository.Load<TModel>(PathToFile))
                .AsSingle()
                .OnInstantiated((context, o) =>
                {
                    onCompleted?.Invoke();
                });
            return Container;
        }

        public DiContainer BindFromNewScriptable<TModel>(IRepository repository, object id, Action onCompleted = null) 
            where TModel : Model
        {
            ConstructName<TModel>();
            
            Container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(repository.Load<TModel>(PathToFile))
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
