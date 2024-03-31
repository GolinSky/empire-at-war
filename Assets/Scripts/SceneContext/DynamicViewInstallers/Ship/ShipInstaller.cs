using EmpireAtWar.Commands.Ship;
using EmpireAtWar.Components.Audio;
using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Views.Ship;
using Zenject;

namespace EmpireAtWar.Ship
{
    public class ShipInstaller : DynamicViewInstaller<ShipController, ShipModel, ShipView>
    {
        private ShipType shipType;
        private PlayerType playerType;

        protected override string ModelPathPrefix => shipType.ToString();
        protected override string ViewPathPrefix => shipType.ToString();

        [Inject]
        public void Construct(ShipType shipType, PlayerType playerType)
        {
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
                .BindInterfaces<ShipMoveComponent>()
                .BindInterfaces<HealthComponent>()
                .BindInterfaces<WeaponComponent>()
                .BindInterfaces<RadarComponent>()
                .BindInterfaces<AiComponent>()
                .BindInterfaces<AudioShipComponent>();
            
            switch (playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindInterfaces<SelectionComponent>();
                    Container.BindInterfaces<PlayerShipCommand>();
                    Container.BindInterfaces<AudioDialogShipComponent>();
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container.BindInterfaces<EnemySelectionComponent>();
                    Container.BindInterfaces<EnemyShipCommand>();
                    break;
                }
            }
        }
    }
}