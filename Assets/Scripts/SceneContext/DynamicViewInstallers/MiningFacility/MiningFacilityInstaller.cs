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
        private PlayerType _playerType;
        private MiningFacilityType _miningFacilityType;
        private IModelMediatorService _modelMediatorService;

        [Inject]
        public void Construct(IModelMediatorService modelMediatorService, PlayerType playerType, MiningFacilityType miningFacilityType)
        {
            _modelMediatorService = modelMediatorService;
            _playerType = playerType;
            _miningFacilityType = miningFacilityType;
           // Debug.Log($"MiningFacilityInstaller: {StartPosition}");

        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(_playerType);
            Container.BindEntity(_miningFacilityType);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container
                .BindInterfacesExt<HealthComponent>()
                .BindInterfacesExt<SimpleMoveComponent>()// todo: make non lazy for enemy
                .BindInterfacesExt<RadarComponent>();

            switch (_playerType)
            {
                case PlayerType.Player:
                    Container.BindInterfacesExt<PlayerSelectionComponent>();
                    break;
                case PlayerType.Opponent:
                    Container.BindInterfacesExt<EnemySelectionComponent>();
                    break;
            }
        }
        
        protected override void OnModelCreated()
        {
            base.OnModelCreated();
            _modelMediatorService.AddUnit(Container.Resolve<MiningFacilityModel>());
        }
    }
}