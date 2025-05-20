using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Components.StateMachine;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using Zenject;

namespace EmpireAtWar.SpaceStation
{
    public class SpaceStationInstaller : DynamicViewInstaller<SpaceStationController, SpaceStationModel, SpaceStationView>
    {
        private FactionType _factionType;
        private PlayerType _playerType;

        protected override string ViewPathPrefix => _factionType.ToString();
        

        [Inject]
        public void Construct(FactionType factionType, PlayerType playerType)
        {
            _factionType = factionType;
            _playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(_playerType);
            Container.BindEntity(_factionType);
            Container.BindEntity(SelectionType.Base);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            switch (_playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindInterfacesExt<PlayerSelectionComponent>();
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container.BindInterfacesExt<EnemySelectionComponent>();
                    break;
                }
            }

            
            Container
                .BindInterfacesExt<HealthComponent>()
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<WeaponComponent>()
                .BindInterfacesNonLazyExt<SimpleMoveComponent>()
                .BindInterfacesNonLazyExt<UnitStateMachineComponent>();
            
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