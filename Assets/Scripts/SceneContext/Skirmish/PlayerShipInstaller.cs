using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.Ship.Selection;
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
        private readonly PlayerType playerType;

        public PlayerShipInstaller(IRepository repository, ShipType shipType, PlayerType playerType)
        {
            this.repository = repository;
            this.shipType = shipType;
            this.playerType = playerType;
        }
        
        public override void InstallBindings()
        {
            switch (playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindInterfacesAndSelfTo<MoveComponent>()
                        .AsSingle()
                        .NonLazy();
                    Container.BindShipEntity<ShipController, ShipView, ShipModel, PlayerShipCommand>(repository, shipType);
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container.BindInterfacesAndSelfTo<MoveComponent>()
                        .AsSingle()
                        .NonLazy();
                    Container.BindShipEntity<ShipController, ShipView, ShipModel, EnemyShipCommand>(repository, shipType);
                    break;
                }
            }
            
           
        }
    }
}