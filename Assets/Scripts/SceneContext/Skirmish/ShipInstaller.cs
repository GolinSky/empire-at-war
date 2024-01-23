using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.Ship;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Repository;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class ShipInstaller:Installer
    {
        private readonly IRepository repository;
        private readonly ShipType shipType;

        public ShipInstaller(IRepository repository, ShipType shipType)
        {
            this.repository = repository;
            this.shipType = shipType;
        }
        
        public override void InstallBindings()
        {
            BindEntity<ShipController, ShipView, ShipModel, ShipCommand>();

            void BindEntity<TController, TView, TModel, TCommand>()
                where TController : Controller<TModel>
                where TView : IView
                where TModel : Model
                where TCommand : Command
            {
                Container.BindInstance(shipType)
                    .AsSingle();
                   
                Container.BindInterfacesAndSelfTo<TCommand>()
                    .AsSingle();

                Container
                    .BindInterfacesAndSelfTo<TModel>()
                    .FromInstance(Object.Instantiate(repository.Load<TModel>($"{shipType}{(typeof(TModel).Name)}")))
                    .AsSingle();

                Container
                    .BindInterfacesAndSelfTo<TController>()
                    .AsSingle();

                Container
                    .BindInterfacesAndSelfTo<TView>()
                    .FromComponentInNewPrefab(repository.Load<GameObject>($"{shipType}{(typeof(TView).Name)}"))
                    .AsSingle();
            }
        }
        
    }
}