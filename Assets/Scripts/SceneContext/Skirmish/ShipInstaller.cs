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
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class ShipInstaller:Installer
    {
        private readonly IRepository repository;
        private readonly ShipType shipType;
        private readonly PlayerType playerType;
        private readonly Vector3 startPosition;

        public ShipInstaller(IRepository repository, ShipType shipType, PlayerType playerType, Vector3 startPosition)
        {
            this.repository = repository;
            this.shipType = shipType;
            this.playerType = playerType;
            this.startPosition = startPosition;
        }
        
        public override void InstallBindings()
        {
            Container.BindEntity(startPosition);
            Container.BindEntity(playerType);

            Container
                .BindInterfaces<MoveComponent>()
                .BindInterfaces<HealthComponent>()
                .BindInterfaces<WeaponComponent>()
                .BindInterfaces<RadarComponent>()
                .BindInterfaces<AiComponent>();

            Container.BindEntity(shipType);

            switch (playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindInterfaces<SelectionComponent>();
                    Container.BindInterfaces<PlayerShipCommand>();
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container.BindInterfaces<EnemySelectionComponent>();
                    Container.BindInterfaces<EnemyShipCommand>();
                    break;
                }
            }
            
            Container
                .BindModel<ShipModel>(repository, shipType.ToString())
                .BindInterfaces<ShipController>()
                .BindViewFromNewComponent<ShipView>(repository, shipType.ToString());
        }
    }
}