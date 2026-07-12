using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.StateMachine;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.NavigationService;
using Zenject;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.SpaceStation
{
    public class SpaceStationInstaller : DynamicViewInstaller<SpaceStationEntity, SpaceStationModel, SpaceStationEntity>
    {
        private FactionType _factionType;
        private PlayerType _playerType;

        protected override string ViewPathPrefix => _factionType.ToString();
        protected override string ViewPathPostfix => "View";
        

        [Inject]
        public void Construct(FactionType factionType, PlayerType playerType)
        {
            _factionType = factionType;
            _playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_factionType);
            Container.BindEntityExt(SelectionType.Base);
            Container.BindInterfacesTo<EntityComponentData>()
                .FromInstance(Repository.Load<SpaceStationModel>(nameof(SpaceStationModel)).ComponentData);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container.Bind<HealthModel>()
                .FromMethod(_ => Container.Resolve<SpaceStationModel>().GetModel<HealthModel>())
                .AsCached();

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            switch (_playerType)
            {
                case PlayerType.Player:
                {
                    Container.BindInterfacesExt<PlayerSelectionComponent>();
                    break;
                }
                case PlayerType.Opponent:
                {
                    Container.BindInterfacesExt<EnemySelectionComponent>();
                    break;
                }
            }

            
            Container
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<AttackComponent>()
                .BindInterfacesNonLazyExt<UnitStateMachineComponent>();
            
            //entity commands
            Container
                .BindInterfacesExt<SelectionCommand>()
                .BindInterfacesExt<HealthCommand>();
        }
        
        
        protected override void OnViewCreated()
        {
            base.OnViewCreated();
            Container.Install<EntityInstaller>(new object[] { View });
        }

        protected override void BindController()
        {
        }
    }
}
