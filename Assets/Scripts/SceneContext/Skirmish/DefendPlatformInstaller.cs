using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.DefendPlatform;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.DefendPlatform;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class DefendPlatformInstaller:Installer
    {
        private readonly IRepository repository;
        private readonly PlayerType playerType;
        private readonly DefendPlatformType miningFacilityType;
        private readonly Vector3 startPosition;
        
        public DefendPlatformInstaller(
            IRepository repository,
            PlayerType playerType,
            DefendPlatformType miningFacilityType,
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
                .BindInterfaces<RadarComponent>()
                .BindInterfaces<WeaponComponent>();
            
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
                .BindModel<DefendPlatformModel>(repository)
                .BindInterfaces<DefendPlatformController>()
                .BindViewFromNewComponent<DefendPlatformView>(repository);
        }
    }
}