using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class ShipInstaller:Installer
    {
        private readonly ShipType shipType;
        private readonly PlayerType playerType;
        private readonly Vector3 startPosition;

        public ShipInstaller(ShipType shipType, PlayerType playerType, Vector3 startPosition)
        {
            this.shipType = shipType;
            this.playerType = playerType;
            this.startPosition = startPosition;
        }
        
        public override void InstallBindings()
        {
            Container
                .BindInstance(startPosition)
                .AsSingle();

            Container
                .BindInstance(playerType)
                .AsSingle();
            

            Container
                .BindSingle<MoveComponent>()
                .BindSingle<HealthComponent>()
                .BindSingle<WeaponComponent>()
                .BindSingle<RadarComponent>()
                .BindSingle<AiComponent>();
            
            
            switch (playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindSingle<SelectionComponent>();

                    Container.BindShipEntity<ShipController, ShipView, ShipModel, PlayerShipCommand>(shipType);
                    break;
                }
                case PlayerType.Opponent:
                {

                    Container.BindSingle<EnemySelectionComponent>();
                    
                    Container.BindShipEntity<ShipController, ShipView, ShipModel, EnemyShipCommand>(shipType);
                    break;
                }
            }
        }
    }
}