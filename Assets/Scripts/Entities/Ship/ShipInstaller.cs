using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.Audio;
using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Entities.ModelMediator;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
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
                    Container.BindInterfacesExt<PlayerShipCommand>();
                    Container.BindInterfacesExt<AudioDialogShipComponent>();
                    Container.BindInterfacesExt<PlayerStateComponent>();

                    break;
                }
                case PlayerType.Opponent:
                {
                    Container.BindInterfacesExt<EnemySelectionComponent>();
                    Container.BindInterfacesExt<EnemyShipCommand>();
                    Container.BindInterfacesExt<EnemyStateComponent>();
                    break;
                }
            }
        }

        protected override void OnModelCreated()
        {
            base.OnModelCreated();
            _modelMediatorService.AddUnit(Container.Resolve<ShipModel>());
        }
    }
}