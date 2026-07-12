using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.EntityCommands;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using Zenject;

namespace EmpireAtWar.Ship
{
    public sealed class ShipInstaller : DynamicViewInstaller<Ship, ShipModel, Ship>
    {
        private ShipType _shipType;
        private PlayerType _playerType;
        private string shipDataPath;

        protected override string ModelPathPrefix => _shipType.ToString();
        protected override string ViewPathPrefix => _shipType.ToString();
        protected override string ViewPathPostfix => "View";

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
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_shipType);
            Container.BindEntityExt(SelectionType.Ship);


            Container.BindScriptableObject<ShipData>(Repository, path: shipDataPath);
            Container.Bind<WeaponModel>().AsSingle();
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container
                .BindInterfacesExt<ShipMoveComponent>()
                //.BindInterfacesExt<AttackComponent>()
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<AudioShipComponent>();

            Container.BindInterfacesAndSelfTo<StateMachine1>().AsSingle();
            Container.BindInterfacesAndSelfTo<AttackTargetState>().AsSingle();
            Container.BindInterfacesAndSelfTo<PatrolState>().AsSingle();
            Container.BindInterfacesAndSelfTo<FleeState>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipAIBrain>().AsSingle();

            switch (_playerType)
            {
                case PlayerType.Player:
                    {
                        Container.BindInterfacesExt<PlayerShipMediator>();


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

        protected override void BindController()
        {
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

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
        }
    }
}
