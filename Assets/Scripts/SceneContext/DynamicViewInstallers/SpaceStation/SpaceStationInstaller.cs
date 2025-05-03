using EmpireAtWar.Commands.SpaceStation;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.ModelMediator;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.SpaceStation;
using Zenject;

namespace EmpireAtWar.SpaceStation
{
    public class SpaceStationInstaller : DynamicViewInstaller<SpaceStationController, SpaceStationModel, SpaceStationView>
    {
        private FactionType _factionType;
        private PlayerType _playerType;
        private IModelMediatorService _modelMediatorService;

        protected override string ViewPathPrefix => _factionType.ToString();
        

        [Inject]
        public void Construct(IModelMediatorService modelMediatorService, FactionType factionType, PlayerType playerType)
        {
            _modelMediatorService = modelMediatorService;
            _factionType = factionType;
            _playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntity(_playerType);
            Container.BindEntity(_factionType);
            Container.BindEntity(SelectionType.Base);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            switch (_playerType)
            {
                case PlayerType.Player:
                {
                    Container
                        .BindInterfacesExt<SpaceStationCommand>()
                        .BindInterfacesExt<PlayerSelectionComponent>();
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container
                        .BindInterfacesExt<EnemySpaceStationCommand>()
                        .BindInterfacesExt<EnemySelectionComponent>();
                    break;
                }
            }
            
            //entity commands
            Container.BindInterfacesExt<SelectionCommand>();

            
            
            Container
                .BindInterfacesExt<HealthComponent>()
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<WeaponComponent>();
            
            Container.BindInterfacesNonLazyExt<SimpleMoveComponent>();
        }
        
           
        protected override void OnModelCreated()
        {
            base.OnModelCreated();
            _modelMediatorService.AddUnit(Container.Resolve<SpaceStationModel>());
        }
        
        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            Container.Install<EntityInstaller>(new object[] { View });
        }
    }
}