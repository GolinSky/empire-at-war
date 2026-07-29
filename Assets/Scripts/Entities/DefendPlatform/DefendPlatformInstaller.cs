using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.StateMachine;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using Zenject;

namespace EmpireAtWar
{
    public class DefendPlatformInstaller : DynamicEntityInstaller<DefendPlatform, DefendPlatformModel>
    {
        private PlayerType _playerType;
        private DefendPlatformType _miningFacilityType;

        protected override string PrefabPathPostfix => "View";

        [Inject]
        public void Constructor(DefendPlatformType miningFacilityType, PlayerType playerType)
        {
            _miningFacilityType = miningFacilityType;
            _playerType = playerType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_miningFacilityType);
            Container.BindEntityExt(SelectionType.DefendPlatform);
            Container.BindInterfacesTo<EntityComponentData>()
                .FromInstance(Repository.Load<DefendPlatformModel>(nameof(DefendPlatformModel)).ComponentData);
            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            DefendPlatformModel model = Container.Resolve<DefendPlatformModel>();
            BindBuffer(model.HealthModel);
            Container.Bind<IHealthModelObserver>().To<HealthModel>().FromResolve();
            BindBuffer(model.AttackModel);
            Container.Bind<IAttackModelObserver>().To<AttackModel>().FromResolve();
            BindBuffer(model.RadarModel);
            Container.Bind<IRadarModelObserver>().To<RadarModel>().FromResolve();

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container.BindInterfacesAndSelfTo<RadarComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<AttackComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<UnitStateMachineComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<SelectionComponent>()
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
