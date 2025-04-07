using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.DefendPlatform;
using EmpireAtWar.Entities.ModelMediator;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.DefendPlatform;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class DefendPlatformInstaller : DynamicViewInstaller<DefendPlatformController, DefendPlatformModel, DefendPlatformView>
    {
        private PlayerType playerType;
        private DefendPlatformType miningFacilityType;
        private IModelMediatorService modelMediatorService;

        [Inject]
        public void Constructor(IModelMediatorService modelMediatorService, DefendPlatformType miningFacilityType, PlayerType playerType)
        {
            this.modelMediatorService = modelMediatorService;
            this.miningFacilityType = miningFacilityType;
            this.playerType = playerType;
            
            Debug.Log($"DefendPlatformInstaller: {StartPosition}");
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(playerType);
            Container.BindEntity(miningFacilityType);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container
                .BindInterfacesExt<HealthComponent>()
                .BindInterfacesExt<SimpleMoveComponent>()
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<WeaponComponent>();
            
            switch (playerType)
            {
                case PlayerType.Player:
                    Container.BindInterfacesExt<SelectionComponent>();
                    break;
                case PlayerType.Opponent:
                    Container.BindInterfacesExt<EnemySelectionComponent>();
                    break;
            }
        }
        
        protected override void OnModelCreated()
        {
            base.OnModelCreated();
            modelMediatorService.AddUnit(Container.Resolve<DefendPlatformModel>());
        }
    }
}