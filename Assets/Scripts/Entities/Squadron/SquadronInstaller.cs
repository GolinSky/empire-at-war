using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Ship.EntityFacades.Selection;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using Zenject;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.Squadrons
{
    public sealed class SquadronInstaller : DynamicEntityInstaller<Squadron, SquadronData>
    {
        private SquadronType _squadronType;
        private PlayerType _playerType;
        private Quaternion _startRotation;

        protected override string ModelPathPrefix => _squadronType.ToString();
        protected override string PrefabPathPrefix => _squadronType.ToString();
        protected override string PrefabPathPostfix => "View";

        [Inject]
        public void Construct(PlayerType playerType, SquadronType squadronType, Quaternion startRotation)
        {
            _playerType = playerType;
            _squadronType = squadronType;
            _startRotation = startRotation;
        }

        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_squadronType);
            Container.BindEntityExt(_startRotation);
            Container.BindEntityExt(SelectionType.Ship);

            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
            Container.Bind<WeaponModel>().AsSingle();
            Container.Bind<CombatModifiers>().AsSingle();
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container.BindInitializableExecutionOrder<SquadronHealthComponent>(-100);
            Container.BindInitializableExecutionOrder<SquadronFlightComponent>(-90);
            Container.Bind<SquadronHealthModel>().AsSingle();
            Container.Bind<SquadronFlightModel>().AsSingle();
            Container.Bind<RadarModel>().AsSingle();
            Container.Bind<IRadarModelObserver>().To<RadarModel>().FromResolve();
            Container.Bind<UnitOrderModel>().AsSingle();
            Container.Bind<SquadronPilot>().AsSingle();
            Container.Bind<SquadronTargetSelector>().AsSingle();

            Container.BindInterfacesAndSelfTo<SquadronHealthComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<SquadronFlightComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<WeaponComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<RadarComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<SelectionComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<SquadronIconComponent>().FromComponentInHierarchy().AsCached();
            if (_playerType == PlayerType.Opponent)
                Container.BindInterfacesAndSelfTo<FogVisibilityComponent>().FromComponentInHierarchy().AsCached();

            Container.BindInterfacesExt<SquadronOrderFacade>();
            Container.BindInterfacesExt<SelectionFacade>();
            Container.BindInterfacesExt<HealthFacade>();
        }

        protected override void OnEntityCreated()
        {
            base.OnEntityCreated();
            Container.Install<EntityInstaller>(new object[] { Entity });
        }
    }
}
