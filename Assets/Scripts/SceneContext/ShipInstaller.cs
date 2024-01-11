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
        private readonly RepublicShipType republicShipType;

        public ShipInstaller(IRepository repository, RepublicShipType republicShipType)
        {
            this.repository = repository;
            this.republicShipType = republicShipType;
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
                Container.BindInstance(republicShipType)
                    .AsSingle();
                   
                Container.BindInterfacesAndSelfTo<TCommand>()
                    .AsSingle();

                Container
                    .BindInterfacesAndSelfTo<TModel>()
                    .FromInstance(Object.Instantiate(repository.Load<TModel>($"{republicShipType}{(typeof(TModel).Name)}")))
                    .AsSingle();

                Container
                    .BindInterfacesAndSelfTo<TController>()
                    .AsSingle();

                Container
                    .BindInterfacesAndSelfTo<TView>()
                    .FromComponentInNewPrefab(repository.Load<GameObject>($"{republicShipType}{(typeof(TView).Name)}"))
                    .AsSingle();
            }
        }
        
    }
}