using EmpireAtWar.Components.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.StateMachine;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.NavigationService;
using Zenject;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.MiningFacility
{
    public class MiningFacilityInstaller : DynamicViewInstaller<MiningFacilityEntity, MiningFacilityModel,
        MiningFacilityEntity>
    {
        private PlayerType _playerType;
        private MiningFacilityType _miningFacilityType;

        protected override string ViewPathPostfix => "View";

        [Inject]
        public void Construct(PlayerType playerType, MiningFacilityType miningFacilityType)
        {
            _playerType = playerType;
            _miningFacilityType = miningFacilityType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_miningFacilityType);
            Container.BindEntityExt(SelectionType.MiningFacility);
            Container.BindInterfacesTo<EntityComponentData>()
                .FromInstance(Repository.Load<MiningFacilityModel>(nameof(MiningFacilityModel)).ComponentData);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container.Bind<HealthModel>()
                .FromMethod(_ => Container.Resolve<MiningFacilityModel>().GetModel<HealthModel>())
                .AsCached();

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container.BindInterfacesExt<RadarComponent>();
            
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
            Container
                .BindInterfacesExt<SelectionCommand>()
                .BindInterfacesExt<HealthCommand>();

        }
        
        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            Container.Install<EntityInstaller>(new object[] { View });
        }

        protected override void BindController()
        {
        }

    }
}
