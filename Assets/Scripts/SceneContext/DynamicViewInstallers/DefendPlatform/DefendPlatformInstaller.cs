using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.DefendPlatform;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.DefendPlatform;
using Zenject;

namespace EmpireAtWar
{
    public class DefendPlatformInstaller : DynamicViewInstaller<DefendPlatformController, DefendPlatformModel, DefendPlatformView>
    {
        private PlayerType playerType;
        private DefendPlatformType miningFacilityType;

        [Inject]
        public void Constructor(DefendPlatformType miningFacilityType, PlayerType playerType)
        {
            this.miningFacilityType = miningFacilityType;
            this.playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(playerType);
            Container.BindEntity(miningFacilityType);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container
                .BindInterfaces<HealthComponent>()
                .BindInterfaces<SimpleMoveComponent>()
                .BindInterfaces<RadarComponent>()
                .BindInterfaces<WeaponComponent>();
            
            switch (playerType)
            {
                case PlayerType.Player:
                    Container.BindInterfaces<SelectionComponent>();
                    break;
                case PlayerType.Opponent:
                    Container.BindInterfaces<EnemySelectionComponent>();
                    break;
            }
        }
    }
}