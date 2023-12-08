using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.Ship;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework;
using WorkShop.LightWeightFramework.Command;
using Zenject;

public class EntityMonoInstaller : Installer
{


    public override void InstallBindings()
    {
       // BindEntity<ShipController, ShipView, ShipModel, ShipCommand>();

        
        void BindEntity<TController, TView, TModel, TCommand>()
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
            where TCommand : Command
        {
            Container.BindInterfacesAndSelfTo<TCommand>()
                .AsTransient();

            Container
                .BindInterfacesAndSelfTo<TModel>()
                .AsTransient();

            Container
                .BindInterfacesTo<TView>()
                .AsTransient();

            Container
                .BindInterfacesAndSelfTo<TController>()
                .AsTransient();
        }
        
        void BindEntityNoCommand<TController, TView, TModel>(TModel model, TView view)
            where TController : Controller<TModel>
            where TView : IView
            where TModel : Model
        {
            Container
                .BindInterfacesAndSelfTo<TModel>()
                .FromInstance(model)
                .AsSingle();

            Container
                .BindInterfacesTo<TView>()
                .FromInstance(view)
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
        }
    }
}