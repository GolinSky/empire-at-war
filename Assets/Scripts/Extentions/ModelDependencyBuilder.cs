﻿using System;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Model;
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
                    HandleModel<TModel>(context, o);
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
                    HandleModel<TModel>(context, o);
                    onCompleted?.Invoke();
                });
            return Container;
        }
         
        private void HandleModel<TModel>(InjectContext context, object @object)
        {
            Model model = (Model)@object;
            foreach (IModel currentModel in model.CurrentModels)
            {
                context.Container.Inject(currentModel);
            }
        }
        
        public static ModelDependencyBuilder ConstructBuilder(DiContainer container)
        {
            return new ModelDependencyBuilder(container);
        }
    }
}