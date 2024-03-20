using EmpireAtWar.Commands.SpaceStation;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Views.SpaceStation;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class StationInstaller : Installer
    {
        private readonly IRepository repository;
        private readonly FactionType factionType;
        private readonly PlayerType playerType;
        private readonly Vector3 startPosition;

        public StationInstaller(IRepository repository, FactionType factionType, PlayerType playerType, Vector3 startPosition)
        {
            this.repository = repository;
            this.factionType = factionType;
            this.playerType = playerType;
            this.startPosition = startPosition;
        }

        public override void InstallBindings()
        {
            Container.BindEntity(startPosition);
            Container.BindEntity(playerType);

            Container
                .BindInterfaces<HealthComponent>()
                .BindInterfaces<SimpleMoveComponent>()
                .BindInterfaces<RadarComponent>()
                .BindInterfaces<WeaponComponent>();

            switch (playerType)
            {
                case PlayerType.Player:
                {
                    Container
                        .BindInterfaces<SpaceStationCommand>()
                        .BindInterfaces<SelectionComponent>();
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container
                        .BindInterfaces<EnemySpaceStationCommand>()
                        .BindInterfaces<EnemySelectionComponent>();
                    break;
                }
            }

            Container
                .BindModel<SpaceStationModel>(repository)
                .BindInterfaces<SpaceStationController>()
                .BindViewFromNewComponent<SpaceStationView>(repository, factionType.ToString());
        }
    }
}