using System;
using LightWeightFramework.Model;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public static class InstallerExtensions
    {
        //todo: rename it 
        public static DiContainer BindInterfacesExt<TEntity>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle();
            return container;
        }
        
        public static DiContainer BindInterfacesExt<TEntity>(this DiContainer container, object id)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle()
                .WithConcreteId(id);
            
            return container;
        }

        
        public static DiContainer BindInterfacesNonLazyExt<TEntity>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle()
                .NonLazy();
            return container;
        }
        
        public static DiContainer BindModel<TModel>(
            this DiContainer container,
            IRepository repository,
            string prefix = null,
            string postfix = null)
         where TModel: Model
        {
            ModelDependencyBuilder
                .ConstructBuilder(container)
                .AppendToPath(prefix, postfix)
                .BindFromNewScriptable<TModel>(repository);
            return container;
        }
        
       

      
        public static ConcreteIdArgConditionCopyNonLazyBinder BindEntity<TEntity>(this DiContainer container, TEntity entity)
        {
            var binder =  container
                .BindInstance(@entity)
                .AsSingle();
            return binder;
        }
        
        private static string ConstructName<T>()
        {
            return typeof(T).Name;
        }
    }
}