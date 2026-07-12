using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.StateMachine;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.NavigationService;
using Zenject;

namespace EmpireAtWar
{
    public class DefendPlatformInstaller : DynamicViewInstaller<DefendPlatformController, DefendPlatformModel, DefendPlatformView>
    {
        private PlayerType _playerType;
        private DefendPlatformType _miningFacilityType;

        [Inject]
        public void Constructor(DefendPlatformType miningFacilityType, PlayerType playerType)
        {
            _miningFacilityType = miningFacilityType;
            _playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_miningFacilityType);
            Container.BindEntityExt(SelectionType.DefendPlatform);
            Container.BindInterfacesTo<EntityComponentData>()
                .FromInstance(Repository.Load<DefendPlatformModel>(nameof(DefendPlatformModel)).ComponentData);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container.Bind<HealthModel>()
                .FromMethod(_ => Container.Resolve<DefendPlatformModel>().GetModel<HealthModel>())
                .AsCached();

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<AttackComponent>()
                .BindInterfacesNonLazyExt<UnitStateMachineComponent>();
            
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
    }
}
