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
        private PlayerType _playerType;
        private DefendPlatformType _miningFacilityType;
        private IModelMediatorService _modelMediatorService;

        [Inject]
        public void Constructor(IModelMediatorService modelMediatorService, DefendPlatformType miningFacilityType, PlayerType playerType)
        {
            _modelMediatorService = modelMediatorService;
            _miningFacilityType = miningFacilityType;
            _playerType = playerType;
            
            Debug.Log($"DefendPlatformInstaller: {StartPosition}");
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
                .BindInterfacesExt<SimpleMoveComponent>()
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<WeaponComponent>();
            
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
            _modelMediatorService.AddUnit(Container.Resolve<DefendPlatformModel>());
        }
    }
}