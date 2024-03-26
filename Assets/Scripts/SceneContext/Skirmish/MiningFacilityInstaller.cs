using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.MiningFacility;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Views.MiningFacility;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class MiningFacilityInstaller : Installer
    {
        private readonly IRepository repository;
        private readonly PlayerType playerType;
        private readonly MiningFacilityType miningFacilityType;
        private readonly Vector3 startPosition;
        
        public MiningFacilityInstaller(
            IRepository repository,
            PlayerType playerType,
            MiningFacilityType miningFacilityType,
            Vector3 startPosition)
        {
            this.repository = repository;
            this.playerType = playerType;
            this.miningFacilityType = miningFacilityType;
            this.startPosition = startPosition;
        }

        public override void InstallBindings()
        {
            Container.BindEntity(startPosition);
            Container.BindEntity(playerType);
            
            Container
                .BindInterfaces<HealthComponent>()
                .BindInterfaces<SimpleMoveComponent>()
                .BindInterfaces<RadarComponent>();

            switch (playerType)
            {
                case PlayerType.Player:
                    Container.BindInterfaces<SelectionComponent>();
                    break;
                case PlayerType.Opponent:
                    Container.BindInterfaces<EnemySelectionComponent>();
                    break;
            }
            
            Container
                .BindModel<MiningFacilityModel>(repository)
                .BindInterfaces<MiningFacilityController>()
                .BindViewFromNewComponent<MiningFacilityView>(repository);
        }
    }
}