using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.AttackComponent;
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
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using Zenject;

namespace EmpireAtWar.Ship
{
    public sealed class ShipInstaller : DynamicEntityInstaller<Ship, ShipComponentsData>
    {
        private ShipType _shipType;
        private PlayerType _playerType;
        private string shipDataPath;

        protected override string ModelPathPrefix => _shipType.ToString();
        protected override string PrefabPathPrefix => _shipType.ToString();
        protected override string PrefabPathPostfix => "View";

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
            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
            Container.Bind<AudioShipData>()
                .FromNewScriptableObject(Repository.Load<AudioShipData>(nameof(AudioShipData)))
                .AsSingle();
            Container.Bind<ILateDisposable>().To<AudioShipData>().FromResolve();
            Container.Bind<AudioShipModel>().AsSingle();
            Container.Bind<IAudioShipModelObserver>().To<AudioShipModel>().FromResolve();

            if (_playerType == PlayerType.Player)
            {
                Container.Bind<AudioShipDialogData>()
                    .FromNewScriptableObject(Repository.Load<AudioShipDialogData>(nameof(AudioShipDialogData)))
                    .AsSingle();
                Container.Bind<AudioShipDialogModel>().AsSingle();
                Container.Bind<IAudioShipDialogModelObserver>().To<AudioShipDialogModel>().FromResolve();
            }

            Container.Bind<WeaponModel>().AsSingle();
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            ShipComponentsData model = Container.Resolve<ShipComponentsData>();
            BindBuffer(model.HealthModel);
            Container.Bind<IHealthModelObserver>().To<HealthModel>().FromResolve();
            BindBuffer(model.ShipMoveModel);
            Container.Bind<IShipMoveModelObserver>().To<ShipMoveModel>().FromResolve();
            BindBuffer(model.AttackModel);
            Container.Bind<IAttackModelObserver>().To<AttackModel>().FromResolve();
            BindBuffer(model.RadarModel);
            Container.Bind<IRadarModelObserver>().To<RadarModel>().FromResolve();

            Container
                .BindInterfacesAndSelfTo<WeaponComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container.BindInterfacesAndSelfTo<ShipMoveComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<RadarComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<AudioShipComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<SelectionComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container.BindInterfacesAndSelfTo<StateMachine1>().AsSingle();
            Container.BindInterfacesAndSelfTo<AttackTargetState>().AsSingle();
            Container.BindInterfacesAndSelfTo<IdleState>().AsSingle();
            Container.BindInterfacesAndSelfTo<NavigateState>().AsSingle();
            Container.BindInterfacesAndSelfTo<FleeState>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipAIBrain>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipAiDecisionModel>().AsSingle();

            switch (_playerType)
            {
                case PlayerType.Player:
                    {
                        Container.BindInterfacesExt<PlayerShipCommand>();
                        Container.BindInterfacesAndSelfTo<AudioDialogShipComponent>()
                            .FromComponentsInHierarchy()
                            .AsCached();

                        //entity commands
                        Container.BindInterfacesExt<PlayerAttackShipCommand>();
                        Container.BindInterfacesExt<SelectionCommand>();
                        Container.BindInterfacesExt<HealthCommand>();

                        break;
                    }
                case PlayerType.Opponent:
                    {
                        Container.BindInterfacesExt<EnemyShipCommand>();
                        //entity commands
                        Container.BindInterfacesExt<EnemyAttackShipCommand>();
                        Container.BindInterfacesExt<SelectionCommand>();
                        Container.BindInterfacesExt<HealthCommand>();

                        break;
                    }
            }
        }

        protected override void OnEntityCreated()
        {
            base.OnEntityCreated();
            Container.Install<EntityInstaller>(new object[] { Entity });
        }
    }
}
