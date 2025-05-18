using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.MiningFacility;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Views.MiningFacility;
using Zenject;

namespace EmpireAtWar.MiningFacility
{
    public class MiningFacilityInstaller : DynamicViewInstaller<MiningFacilityController, MiningFacilityModel,
        MiningFacilityView>
    {
        private PlayerType _playerType;
        private MiningFacilityType _miningFacilityType;

        [Inject]
        public void Construct(PlayerType playerType, MiningFacilityType miningFacilityType)
        {
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
            
            //entity commands
            Container.BindInterfacesExt<SelectionCommand>();

        }
        
        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            Container.Install<EntityInstaller>(new object[] { View });
        }

    }
}