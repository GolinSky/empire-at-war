using LightWeightFramework.Model;
using LightWeightFramework.Components.Repository;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public static class InstallerExtensions
    {
        public static DiContainer BindInterfaces<TEntity>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle();
            return container;
        }
        
        public static DiContainer BindInterfaces<TEntity>(this DiContainer container, object id)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle()
                .WithConcreteId(id);

        
            
            return container;
        }

        
        public static DiContainer BindInterfacesNonLazy<TEntity>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TEntity>()
                .AsSingle()
                .NonLazy();
            return container;
        }
        
        public static DiContainer BindModel<TModel>(this DiContainer container, IRepository repository, string prefix = null, string postfix = null)
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
    }
}