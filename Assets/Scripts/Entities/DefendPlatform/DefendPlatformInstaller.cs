using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Ship.EntityFacades.Combat;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Ship.EntityFacades.Selection;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Selection;
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
                .FromInstance(Repository.Load<DefendPlatformData>(ModelPathPrefix + nameof(DefendPlatformData)).ComponentData);
            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
            Container.Bind<WeaponModel>()
                .FromMethod(context => new WeaponModel(
                    context.Container.Resolve<EmpireAtWar.Components.AttackComponent.DamageMatrixData>().CopyAccuracy()))
                .AsSingle();
            Container.Bind<CombatModifiers>().AsSingle();
            Container.BindInterfacesTo<ResearchCombatModifier>().AsSingle();
            Container.Decorate<IHealthData>().With<ResearchHealthData>();
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container.BindInitializableExecutionOrder<HealthComponent>(-100);
            Container.Bind<HealthModel>().AsSingle();
            Container.Bind<RadarModel>().AsSingle();
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
            if (_playerType == PlayerType.Opponent)
                Container.BindInterfacesAndSelfTo<FogVisibilityComponent>()
                    .FromComponentsInHierarchy()
                    .AsCached();
            
            //entity commands
            Container
                .BindInterfacesExt<SelectionFacade>()
                .BindInterfacesExt<HealthFacade>()
                .BindInterfacesExt<HardPointsFacade>()
                .BindInterfacesExt<CombatModifiersFacade>()
                .BindInterfacesExt<StationaryAttackFacade>();
        }
        
        protected override void OnEntityCreated()
        {
            base.OnEntityCreated();
            Container.Install<EntityInstaller>(new object[] { Entity });
        }
    }
}
