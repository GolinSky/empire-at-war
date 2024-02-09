using EmpireAtWar.Models.Factions;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Repository;
using Zenject;

namespace EmpireAtWar.Extentions
{
    public static class InstallerExtensions
    {
        public static void BindEntityFromPrefab<TController, TView, TModel, TCommand>(this DiContainer container, FactionType factionType)
            where TController : Controller<TModel>
            where TView : Component, IView
            where TModel : Model
            where TCommand : Command
        {
            IRepository repository = container.Resolve<IRepository>();

            container.BindInterfacesAndSelfTo<TCommand>()
                .AsSingle();

            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(repository.Load<TModel>($"{typeof(TModel).Name}"))
                .AsSingle()
                .NonLazy();

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
            container.BindInterfacesAndSelfTo<TCommand>()
                .AsSingle();

            container.BindEntity<TController, TView, TModel>(view);
        }
        
        public static void BindEntity<TController, TView, TModel>(this DiContainer container,  TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
        {
            IRepository repository = container.Resolve<IRepository>();
            
            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(repository.Load<TModel>(typeof(TModel).Name))
                .AsSingle();

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
                .NonLazy();
            
            container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
            
            container
                .BindInterfacesAndSelfTo<TView>()
                .FromComponentInNewPrefab(repository.Load<GameObject>($"{shipType}{(typeof(TView).Name)}"))
                .AsSingle();
        }
    }
}