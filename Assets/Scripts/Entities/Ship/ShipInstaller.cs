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
        private ShipType shipType;
        private PlayerType playerType;
        private IModelMediatorService modelMediatorService;

        protected override string ModelPathPrefix => shipType.ToString();
        protected override string ViewPathPrefix => shipType.ToString();

        [Inject]
        public void Construct(IModelMediatorService modelMediatorService, ShipType shipType, PlayerType playerType)
        {
            this.modelMediatorService = modelMediatorService;
            this.shipType = shipType;
            this.playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(playerType);
            Container.BindEntity(shipType);
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
            
            switch (playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindInterfacesExt<SelectionComponent>();
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
            modelMediatorService.AddUnit(Container.Resolve<ShipModel>());
        }
    }
}