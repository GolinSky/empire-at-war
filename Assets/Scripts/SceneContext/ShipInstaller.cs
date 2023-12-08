using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Controllers.Ship;
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
        [Inject]
        private IRepository Repository { get; }
        
        public override void InstallBindings()
        {
            BindEntity<ShipController, ShipView, ShipModel, ShipCommand>();

            void BindEntity<TController, TView, TModel, TCommand>()
                where TController : Controller<TModel>
                where TView : IView
                where TModel : Model
                where TCommand : Command
            {
                Container.BindInterfacesAndSelfTo<TCommand>()
                    .AsSingle();

                Container
                    .BindInterfacesAndSelfTo<TModel>()
                    .FromInstance(Object.Instantiate(Repository.Load<ShipModel>("ShipModel")))
                    .AsSingle();

                Container
                    .BindInterfacesAndSelfTo<TController>()
                    .AsSingle();
                
                Container
                    .BindInterfacesAndSelfTo<TView>()
                    .FromComponentInNewPrefab(Repository.Load<GameObject>("ShipView"))
                    .AsSingle();
            }
        }
    }
}