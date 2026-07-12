using EmpireAtWar.Components.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.StateMachine;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using Zenject;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.MiningFacility
{
    public class MiningFacilityInstaller : DynamicEntityInstaller<MiningFacilityEntity, MiningFacilityModel>
    {
        private PlayerType _playerType;
        private MiningFacilityType _miningFacilityType;

        protected override string PrefabPathPostfix => "View";

        [Inject]
        public void Construct(PlayerType playerType, MiningFacilityType miningFacilityType)
        {
            _playerType = playerType;
            _miningFacilityType = miningFacilityType;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_miningFacilityType);
            Container.BindEntityExt(SelectionType.MiningFacility);
            Container.BindInterfacesTo<EntityComponentData>()
                .FromInstance(Repository.Load<MiningFacilityModel>(nameof(MiningFacilityModel)).ComponentData);
            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            MiningFacilityModel model = Container.Resolve<MiningFacilityModel>();
            BindBuffer(model.HealthModel);
            Container.Bind<IHealthModelObserver>().To<HealthModel>().FromResolve();
            BindBuffer(model.DefaultMoveModel);
            Container.Bind<IDefaultMoveModelObserver>().To<DefaultMoveModel>().FromResolve();
            BindBuffer(model.RadarModel);
            Container.Bind<IRadarModelObserver>().To<RadarModel>().FromResolve();

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container.BindInterfacesAndSelfTo<RadarComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<SimpleMoveComponent>()
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
