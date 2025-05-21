using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.EntityCommands;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Movement;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using Zenject;

namespace EmpireAtWar.Ship
{
    public class ShipInstaller : DynamicViewInstaller<ShipController, ShipModel, ShipView>
    {
        private ShipType _shipType;
        private PlayerType _playerType;

        protected override string ModelPathPrefix => _shipType.ToString();
        protected override string ViewPathPrefix => _shipType.ToString();

        [Inject]
        public void Construct(ShipType shipType, PlayerType playerType)
        {
            _shipType = shipType;
            _playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(_playerType);
            Container.BindEntity(_shipType);
            Container.BindEntity(SelectionType.Ship);
        }

        protected override void BindComponents()
        {
            base.BindComponents();

            
            Container
                .BindInterfacesExt<ShipMoveComponent>()
                .BindInterfacesExt<HealthComponent>()
                .BindInterfacesExt<AttackComponent>()
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<AudioShipComponent>();
            
            switch (_playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindInterfacesExt<PlayerSelectionComponent>();
                    Container.BindInterfacesExt<PlayerShipCommand>();//todo: why we need this
                    Container.BindInterfacesExt<AudioDialogShipComponent>();
                    Container.BindInterfacesExt<PlayerShipStateMachineComponent>();
                    
                    //entity commands
                    Container.BindInterfacesExt<PlayerAttackShipCommand>();
                    Container.BindInterfacesExt<SelectionCommand>();
                    Container.BindInterfacesExt<ShipMovementCommand>();
                    Container.BindInterfacesExt<HealthCommand>();

                    break;
                }
                case PlayerType.Opponent:
                {
                    Container.BindInterfacesExt<EnemySelectionComponent>();
                    Container.BindInterfacesExt<EnemyShipCommand>();
                    Container.BindInterfacesExt<EnemyShipStateComponent>();
                    
                    //entity commands
                    Container.BindInterfacesExt<EnemyAttackShipCommand>();
                    Container.BindInterfacesExt<SelectionCommand>();
                    Container.BindInterfacesExt<HealthCommand>();

                    break;
                }
            }
        }

        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            Container.Install<EntityInstaller>(new object[] { View });
        }
    }
}