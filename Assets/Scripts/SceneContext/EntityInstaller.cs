using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.Ship;
using UnityEngine;
using WorkShop.LightWeightFramework.Repository;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class EntityInstaller : Installer
    {
        [Inject]
        private IRepository Repository { get; }
        
        public override void InstallBindings()
        {
            Container
                .BindFactory<RepublicShipType, ShipView, ShipFacadeFactory>()
                .FromSubContainerResolve()
                .ByNewGameObjectInstaller<ShipInstaller>();
            //   .FromMethod(InstallShip); //first attempt - not working iinitializeble
        }
    
        // private ShipView InstallShip(DiContainer subContainer)
        // {
        //     
        //     ShipModel model = Object.Instantiate(Repository.Load<ShipModel>("ShipModel")) ;
        //
        //     ShipView shipView =
        //         subContainer.InstantiatePrefabForComponent<ShipView>(Repository.Load<ShipView>("ShipView"));
        //
        //     subContainer
        //         .BindInterfacesAndSelfTo<ShipModel>()
        //         .FromMethod((container) => { return model; })
        //         .AsTransient()
        //         .WhenInjectedIntoInstance(shipView);
        //             
        //     subContainer
        //         .BindInterfacesAndSelfTo<ShipEntity>()
        //         .AsTransient()
        //         .WithArguments(model)
        //         .WhenInjectedInto<ShipCommand>();
        //     
        //     subContainer
        //         .BindInterfacesAndSelfTo<ShipCommand>()
        //         .AsTransient()
        //         .WhenInjectedIntoInstance(shipView);
        //             
        //     subContainer
        //         .BindInterfacesAndSelfTo<ShipView>()
        //         .AsTransient()
        //         .WhenInjectedIntoInstance(shipView);
        //             
        //     // todo Initialize    
        //     subContainer.Inject(shipView);
        //     subContainer.Inject(model);
        //     // subContainer.FlushBindings();
        //     subContainer.Unbind<ShipCommand>();
        //     subContainer.Unbind<ShipEntity>();
        //     
        //     return shipView;
        // }
    }
}