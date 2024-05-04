using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.MiningFacility;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Views.MiningFacility;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.MiningFacility
{
    public class MiningFacilityInstaller : DynamicViewInstaller<MiningFacilityController, MiningFacilityModel,
        MiningFacilityView>
    {
        private PlayerType playerType;
        private MiningFacilityType miningFacilityType;

        [Inject]
        public void Construct(PlayerType playerType, MiningFacilityType miningFacilityType)
        {
            this.playerType = playerType;
            this.miningFacilityType = miningFacilityType;
            Debug.Log($"MiningFacilityInstaller: {StartPosition}");

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
                .BindInterfaces<SimpleMoveComponent>()// todo: make non lazy for enemy
                .BindInterfaces<RadarComponent>();

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