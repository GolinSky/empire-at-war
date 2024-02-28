using EmpireAtWar.Models.Factions;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using LightWeightFramework.Components;
using LightWeightFramework.Command;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Components.ViewComponents;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public static class InstallerExtensions
    {
        //todo: rename
        public static void BindEntityFromPrefab<TController, TView, TModel, TCommand>(this DiContainer container, FactionType factionType)
            where TController : Controller<TModel>
            where TView : Component, IView
            where TModel : Model
            where TCommand : Command
        {
            IRepository repository = container.Resolve<IRepository>();

            container.BindInterfacesAndSelfTo<TCommand>()
                .AsSingle();
            
            container.BindModel<TModel>();

            container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
            
            container
                .BindInterfacesAndSelfTo<TView>()
                .FromComponentInNewPrefab(repository.Load<GameObject>($"{factionType}{typeof(TView).Name}"))
                .AsSingle();
        }
        
        public static void BindEntity<TController, TView, TModel, TCommand>(this DiContainer container, TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
            where TCommand : Command
        {
            container
                .BindInterfacesAndSelfTo<TCommand>()
                .AsSingle();

            container.BindEntity<TController, TView, TModel>(view);
        }
        
        public static void BindEntity<TController, TView, TModel>(this DiContainer container,  TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
        {
            container
                .BindModel<TModel>();

            container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
            
            container
                .BindInterfacesTo<TView>()
                .FromInstance(view)
                .AsSingle();
        }
        
        public static void BindEntitySubContainerResolve<TController, TView, TModel>(this DiContainer container,  TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
        {
            container
                .BindModel<TModel>();

            container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
            
            container
                .BindInterfacesTo<TView>()
                .FromInstance(view)
                .AsSingle();
        }
        
        public static void BindShipEntity<TController, TView, TModel, TCommand>(this DiContainer container, ShipType shipType)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
            where TCommand : Command
        {
            IRepository repository = container.Resolve<IRepository>();

            container.BindInstance(shipType)
                .AsSingle();
            
            container.BindInterfacesAndSelfTo<TCommand>()
                .AsSingle();

            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(repository.Load<TModel>($"{shipType}{(typeof(TModel).Name)}"))
                .AsSingle()
                .OnInstantiated((context, o) =>
                {
                    Model model = (Model)o;
                    foreach (IModel currentModel in model.CurrentModels)
                    {
                        context.Container.Inject(currentModel);
                      //  context.Container.BindInterfacesTo(currentModel.GetType()).AsSingle();
                    }
                })
                .NonLazy();
            
            container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
            
            
            container
                .BindInterfacesAndSelfTo<TView>()
                .FromComponentInNewPrefab(repository.Load<GameObject>($"{shipType}{(typeof(TView).Name)}"))
                .AsSingle()
                .OnInstantiated((context, o) =>
                {
                    TView view = (TView)o;
                    foreach (ViewComponent component in view.ViewComponents)
                    {
                        context.Container.Inject(component);
                        context.Container.BindInterfacesTo(component.GetType()).FromComponentOn(component.gameObject).AsSingle();
                    }
                } );

            // TView view = container.InstantiatePrefabForComponent<TView>(
            //     repository.Load<GameObject>($"{shipType}{(typeof(TView).Name)}"));
            // container.BindInterfacesAndSelfTo<TView>().AsSingle();
            
        }


        public static DiContainer BindService<TService>(this DiContainer container)
        {
            container
                .BindInterfacesAndSelfTo<TService>()
                .AsSingle()
                .NonLazy();
            return container;
        }

        public static DiContainer BindModel<TModel>(this DiContainer container) where TModel: ScriptableObject, IModel
        {
            IRepository repository = container.Resolve<IRepository>();

            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(repository.Load<TModel>(typeof(TModel).Name))
                
                .AsSingle();

            return container;
        }

        public static DiContainer BindSingle<TEntity>(this DiContainer container)
        {
            container.BindInterfacesAndSelfTo<TEntity>()
                .AsSingle()
                .NonLazy();
            return container;
        }
    }
}