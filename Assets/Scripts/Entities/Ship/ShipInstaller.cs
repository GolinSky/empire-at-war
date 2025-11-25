using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.EntityCommands;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Movement;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using Zenject;

namespace EmpireAtWar.Ship
{
    public sealed class ShipInstaller : DynamicViewInstaller<ShipController, ShipModel, ShipView>
    {
        private ShipType _shipType;
        private PlayerType _playerType;
        private string shipDataPath;

        protected override string ModelPathPrefix => _shipType.ToString();
        protected override string ViewPathPrefix => _shipType.ToString();

        [Inject]
        public void Construct(ShipType shipType, PlayerType playerType, ShipsData shipsData)
        {
            _shipType = shipType;
            _playerType = playerType;
            shipDataPath = shipsData.GetShipDataPath(shipType);
        }
        

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(_playerType);
            Container.BindEntity(_shipType);
            Container.BindEntity(SelectionType.Ship);

            
            Container.BindScriptableObject<ShipData>(Repository, path: shipDataPath);
            Container.Bind<WeaponModel>().AsSingle();
            // switch (_playerType)
            // {
            //     case PlayerType.Player:
            //     {
            //         Container.BindScriptableObject<ShipData>(Repository, path: shipDataPath);
            //         Container.Bind<WeaponModel>().AsSingle();
            //         break;
            //     }
            //     case PlayerType.Opponent:
            //         break;
            // }
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
                    Container.BindInterfacesExt<PlayerShipMediator>();
                    Container.BindInterfacesExt<AttackTargetState>();
                        
                        
                    Container.BindInterfacesExt<PlayerSelectionComponent>();
                    Container.BindInterfacesExt<PlayerShipCommand>();//todo: why we need this
                    Container.BindInterfacesExt<AudioDialogShipComponent>();
                  //  Container.BindInterfacesExt<PlayerShipStateMachine>();
                    
                    //entity commands
                    Container.BindInterfacesExt<PlayerAttackShipCommand>();
                    Container.BindInterfacesExt<SelectionCommand>();
                  //  Container.BindInterfacesExt<ShipMovementCommand>();
                    Container.BindInterfacesExt<HealthCommand>();

                    break;
                }
                case PlayerType.Opponent:
                {
                    Container.BindInterfacesExt<EnemySelectionComponent>();
                    Container.BindInterfacesExt<EnemyShipCommand>();
                    // Container.BindInterfacesExt<EnemyShipStateMachine>();
                    Container.BindInterfacesExt<EnemyShipMediator>();

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
            
            //debug code - until I apply changes to mvc package
            Container
                .BindInterfacesAndSelfTo<WeaponComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
        }
    }
}