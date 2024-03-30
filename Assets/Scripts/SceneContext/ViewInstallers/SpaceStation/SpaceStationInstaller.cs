using EmpireAtWar.Commands.SpaceStation;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SpaceStation;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class SpaceStationInstaller : BaseViewInstaller<SpaceStationController, SpaceStationModel>
    {
        private IRepository repository;
        private FactionType factionType;
        private PlayerType playerType;
        private Vector3 startPosition;

        [Inject]
        public void Construct(IRepository repository, FactionType factionType, PlayerType playerType,
            Vector3 startPosition)
        {
            this.repository = repository;
            this.factionType = factionType;
            this.playerType = playerType;
            this.startPosition = startPosition;
        }

        public override void InstallBindings()
        {
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
                .BindInterfaces<HealthComponent>()
                .BindInterfaces<SimpleMoveComponent>()
                .BindInterfaces<RadarComponent>()
                .BindInterfaces<WeaponComponent>();
            
            base.InstallBindings();
        }
    }
}