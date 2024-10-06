using EmpireAtWar.Commands.SpaceStation;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Entities.ModelMediator;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Views.SpaceStation;
using Zenject;

namespace EmpireAtWar.SpaceStation
{
    public class SpaceStationInstaller : DynamicViewInstaller<SpaceStationController, SpaceStationModel, SpaceStationView>
    {
        private FactionType factionType;
        private PlayerType playerType;
        private IModelMediatorService modelMediatorService;

        protected override string ViewPathPrefix => factionType.ToString();
        

        [Inject]
        public void Construct(IModelMediatorService modelMediatorService, FactionType factionType, PlayerType playerType)
        {
            this.modelMediatorService = modelMediatorService;
            this.factionType = factionType;
            this.playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(playerType);
            Container.BindEntity(factionType);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            switch (playerType)
            {
                case PlayerType.Player:
                {
                    Container
                        .BindInterfaces<SpaceStationCommand>()
                        .BindInterfaces<SelectionComponent>();
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container
                        .BindInterfaces<EnemySpaceStationCommand>()
                        .BindInterfaces<EnemySelectionComponent>();
                    break;
                }
            }
            
            Container
                .BindInterfaces<HealthComponent>()
                .BindInterfaces<RadarComponent>()
                .BindInterfaces<WeaponComponent>();
            
            Container.BindInterfacesNonLazy<SimpleMoveComponent>();
        }
        
           
        protected override void OnModelCreated()
        {
            base.OnModelCreated();
            modelMediatorService.AddUnit(Container.Resolve<SpaceStationModel>());
        }
    }
}