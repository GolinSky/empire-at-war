using EmpireAtWar.Models.Factions;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using LightWeightFramework.Components;
using LightWeightFramework.Command;
using LightWeightFramework.Components.Repository;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public static class InstallerExtensions
    {
        
        // public static void BindEntity<TController, TView, TModel, TCommand>(this DiContainer container, TView view)
        //     where TController : Controller<TModel>
        //     where TView : IView
        //     where TModel : Model
        //     where TCommand : Command
        // {
        //     container.BindInterfaces<TCommand>();
        //
        //     container.BindEntity<TController, TView, TModel>(view);
        // }
        //
        // public static void BindEntity<TController, TView, TModel>(this DiContainer container,  TView view)
        //     where TController : Controller<TModel>
        //     where TView : IView
        //     where TModel : Model
        // {
        //     IRepository repository = container.Resolve<IRepository>();
        //
        //     container.BindModel<TModel>(repository);
        //
        //     container.BindInterfaces<TController>();
        //
        //     container.BindViewFromInstance(view);
        // }
        

        public static DiContainer BindInterfaces<TEntity>(this DiContainer container)
        {
            container.BindInterfacesAndSelfTo<TEntity>()
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

        public static DiContainer BindViewFromNewComponent<TView>(
            this DiContainer container,
            IRepository repository,
            string prefix = null,
            string postfix = null)
            where TView :  IView
        {
            ViewDependencyBuilder
                .ConstructBuilder(container)
                .AppendToPath(prefix, postfix)
                .BindFromNewComponent<TView>(repository);
            return container;
        }
        
        public static DiContainer BindViewFromInstance<TView>(
            this DiContainer container,
            TView view,
            string prefix = null,
            string postfix = null)
            where TView :  IView
        {
            ViewDependencyBuilder
                .ConstructBuilder(container)
                .AppendToPath(prefix, postfix)
                .BindFromInstance<TView>(view);
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