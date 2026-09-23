using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.EntityCommands.Health;
using EmpireAtWar.Entities.Ship.EntityCommands.Selection;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using Zenject;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.SpaceStation
{
    public class SpaceStationInstaller : DynamicEntityInstaller<SpaceStationEntity, SpaceStationData>
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
                .FromInstance(Repository.Load<SpaceStationData>(nameof(SpaceStationData)).ComponentData);
            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
            Container.Bind<WeaponModel>().AsSingle();
            Container.Bind<CombatModifiers>().AsSingle();
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container.BindInitializableExecutionOrder<HealthComponent>(-100);
            SpaceStationData model = Container.Resolve<SpaceStationData>();
            Container.Bind<HealthModel>().AsSingle();
            BindBuffer(model.RadarModel);
            Container.Bind<IRadarModelObserver>().To<RadarModel>().FromResolve();

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
            Container.BindInterfacesAndSelfTo<WeaponComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<StationCombatPresenter>().AsSingle();
            
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
