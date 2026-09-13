using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
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
    public class DefendPlatformInstaller : DynamicEntityInstaller<DefendPlatform, DefendPlatformData>
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
                .FromInstance(Repository.Load<DefendPlatformData>(nameof(DefendPlatformData)).ComponentData);
            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
            Container.Bind<WeaponModel>().AsSingle();
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container.BindInitializableExecutionOrder<HealthComponent>(-100);
            DefendPlatformData model = Container.Resolve<DefendPlatformData>();
            Container.Bind<HealthModel>().AsSingle();
            BindBuffer(model.RadarModel);
            Container.Bind<IRadarModelObserver>().To<RadarModel>().FromResolve();

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container.BindInterfacesAndSelfTo<RadarComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<WeaponComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<StationCombatPresenter>().AsSingle();
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
