using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.Ship;
using WorkShop.LightWeightFramework.Repository;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class PlayerShipInstaller:Installer
    {
        private readonly IRepository repository;
        private readonly ShipType shipType;

        public PlayerShipInstaller(IRepository repository, ShipType shipType)
        {
            this.repository = repository;
            this.shipType = shipType;
        }
        
        public override void InstallBindings()
        {
            Container.BindShipEntity<ShipController, ShipView, ShipModel, PlayerShipCommand>(repository, shipType);
        }
    }
}