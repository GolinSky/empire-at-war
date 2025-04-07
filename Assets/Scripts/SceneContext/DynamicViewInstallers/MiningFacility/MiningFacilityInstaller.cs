using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.MiningFacility;
using EmpireAtWar.Entities.ModelMediator;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Views.MiningFacility;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.MiningFacility
{
    public class MiningFacilityInstaller : DynamicViewInstaller<MiningFacilityController, MiningFacilityModel,
        MiningFacilityView>
    {
        private PlayerType playerType;
        private MiningFacilityType miningFacilityType;
        private IModelMediatorService modelMediatorService;

        [Inject]
        public void Construct(IModelMediatorService modelMediatorService, PlayerType playerType, MiningFacilityType miningFacilityType)
        {
            this.modelMediatorService = modelMediatorService;
            this.playerType = playerType;
            this.miningFacilityType = miningFacilityType;
           // Debug.Log($"MiningFacilityInstaller: {StartPosition}");

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
                .BindInterfacesExt<SimpleMoveComponent>()// todo: make non lazy for enemy
                .BindInterfacesExt<RadarComponent>();

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
            modelMediatorService.AddUnit(Container.Resolve<MiningFacilityModel>());
        }
    }
}