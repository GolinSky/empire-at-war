using EmpireAtWar.Components.Ship.Selection;
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
        public static void BindEntity<TController, TView, TModel, TCommand>(this DiContainer container, TModel model, TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
            where TCommand : Command
        {
            container.BindInterfacesAndSelfTo<TCommand>()
                .AsTransient();

            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromInstance(model)
                .AsTransient();

            container
                .BindInterfacesTo<TView>()
                .FromInstance(view)
                .AsTransient();

            container
                .BindInterfacesAndSelfTo<TController>()
                .AsTransient();
        }
        
        public static void BindEntityNoCommand<TController, TView, TModel>(this DiContainer container, TModel model, TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
        {
            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromInstance(model)
                .AsSingle();

            container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
            
            container
                .BindInterfacesTo<TView>()
                .FromInstance(view)
                .AsSingle();
        }
        
        
        public static void BindShipEntity<TController, TView, TModel, TCommand>(this DiContainer container, IRepository repository, ShipType shipType)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
            where TCommand : Command
        {
            container.BindInstance(shipType)
                .AsSingle();
            
                   
            container.BindInterfacesAndSelfTo<TCommand>()
                .AsSingle();

            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromInstance(Object.Instantiate(repository.Load<TModel>($"{shipType}{(typeof(TModel).Name)}")))
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