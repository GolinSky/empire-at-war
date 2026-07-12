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
    public class SpaceStationInstaller : DynamicEntityInstaller<SpaceStationEntity, SpaceStationModel>
    {
        private FactionType _factionType;
        private PlayerType _playerType;

        protected override string PrefabPathPrefix => _factionType.ToString();
        protected override string PrefabPathPostfix => "View";
        

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

            Container.BindInterfacesAndSelfTo<SelectionComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<RadarComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<SimpleMoveComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<AttackComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<UnitStateMachineComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            
            //entity commands
            Container
                .BindInterfacesExt<SelectionCommand>()
                .BindInterfacesExt<HealthCommand>();
        }
        
        
        protected override void OnEntityCreated()
        {
            base.OnEntityCreated();
            Container.Install<EntityInstaller>(new object[] { Entity });
        }
    }
}
