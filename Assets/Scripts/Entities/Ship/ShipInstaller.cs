using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.Audio;
using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.ModelMediator;
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
        private IModelMediatorService _modelMediatorService;

        protected override string ModelPathPrefix => _shipType.ToString();
        protected override string ViewPathPrefix => _shipType.ToString();

        [Inject]
        public void Construct(IModelMediatorService modelMediatorService, ShipType shipType, PlayerType playerType)
        {
            _modelMediatorService = modelMediatorService;
            _shipType = shipType;
            _playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(_playerType);
            Container.BindEntity(_shipType);
        }

        protected override void BindComponents()
        {
            base.BindComponents();

            Container.BindEntity(SelectionType.Ship);
            
            Container
                .BindInterfacesExt<ShipMoveComponent>()
                .BindInterfacesExt<HealthComponent>()
                .BindInterfacesExt<WeaponComponent>()
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<AudioShipComponent>();
            
            switch (_playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindInterfacesExt<PlayerSelectionComponent>();
                    Container.BindInterfacesExt<PlayerShipCommand>();//todo: why we need this
                    Container.BindInterfacesExt<AudioDialogShipComponent>();
                    Container.BindInterfacesExt<PlayerStateComponent>();
                    
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
                    Container.BindInterfacesExt<EnemyStateComponent>();
                    
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

        protected override void OnModelCreated()
        {
            base.OnModelCreated();
            _modelMediatorService.AddUnit(Container.Resolve<ShipModel>());
        }
    }
}