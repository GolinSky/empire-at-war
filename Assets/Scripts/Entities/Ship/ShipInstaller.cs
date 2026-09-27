using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades.Combat;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Ship.EntityFacades.Selection;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Audio;
using UnityEngine;
using Zenject;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Ship
{
    public sealed class ShipInstaller : DynamicEntityInstaller<Ship, ShipData>
    {
        private ShipType _shipType;
        private PlayerType _playerType;
        private string shipDataPath;

        protected override string PrefabPathPrefix => _shipType.ToString();
        protected override string PrefabPathPostfix => "View";

        [Inject]
        public void Construct(ShipType shipType, PlayerType playerType, ShipsData shipsData)
        {
            _shipType = shipType;
            _playerType = playerType;
            shipDataPath = shipsData.GetShipDataPath(shipType);
        }


        protected override void OnBindData()
        {
            base.OnBindData();
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_shipType);
            Container.BindEntityExt(SelectionType.Ship);


            Container.Bind<SelectionModel>().AsSingle();
            Container.Bind<ISelectionModelObserver>().To<SelectionModel>().FromResolve();
            Container.Bind<AudioShipData>()
                .FromNewScriptableObject(Repository.Load<AudioShipData>(nameof(AudioShipData)))
                .AsSingle();
            Container.Bind<ILateDisposable>().To<AudioShipData>().FromResolve();
            Container.Bind<AudioShipModel>().AsSingle();
            Container.Bind<IAudioShipModelObserver>().To<AudioShipModel>().FromResolve();

            if (_playerType == PlayerType.Player)
            {
                Container.Bind<AudioShipDialogData>()
                    .FromNewScriptableObject(Repository.Load<AudioShipDialogData>(nameof(AudioShipDialogData)))
                    .AsSingle();
                Container.Bind<AudioShipDialogModel>().AsSingle();
                Container.Bind<IAudioShipDialogModelObserver>().To<AudioShipDialogModel>().FromResolve();
            }

            Container.Bind<WeaponModel>().AsSingle();
            Container.Bind<CombatModifiers>().AsSingle();
            Container.BindInterfacesTo<ResearchCombatModifier>().AsSingle();
            Container.Decorate<IHealthData>().With<ResearchHealthData>();
        }

        protected override void BindModel()
        {
            Container.BindScriptableObject<ShipData>(Repository, path: shipDataPath);
        }

        protected override void BindComponents()
        {
            base.BindComponents();
            Container.BindInitializableExecutionOrder<HealthComponent>(-100);
            Container.Bind<HealthModel>().AsSingle();
            Container.Bind<ShipMoveModel>().AsSingle();
            Container.Bind<RadarModel>().AsSingle();
            Container.Bind<IRadarModelObserver>().To<RadarModel>().FromResolve();

            Container
                .BindInterfacesAndSelfTo<WeaponComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container
                .BindInterfacesAndSelfTo<HealthComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container.BindInterfacesAndSelfTo<ShipMoveComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<RadarComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<AudioShipComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
            Container.BindInterfacesAndSelfTo<SelectionComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            if (Container.Resolve<ShipData>().HangarBays.Count > 0)
            {
                Container.Bind<HangarModel>().AsSingle();
                Container.BindInterfacesAndSelfTo<HangarComponent>()
                    .FromComponentInHierarchy()
                    .AsCached();
            }

            Container.Bind<ShipStateMachine>().AsSingle();
            Container.BindInterfacesAndSelfTo<AttackTargetState>().AsSingle();
            Container.BindInterfacesAndSelfTo<IdleState>().AsSingle();
            Container.BindInterfacesAndSelfTo<NavigateState>().AsSingle();
            Container.BindInterfacesAndSelfTo<AttackMoveState>().AsSingle();
            Container.BindInterfacesAndSelfTo<GuardState>().AsSingle();
            Container.BindInterfacesAndSelfTo<HuntState>().AsSingle();
            Container.Bind<UnitOrderModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<FleeState>().AsSingle();
            Container.Bind<ShipAIBrain>().AsSingle();
            Container.Bind<ShipOrderRunner>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipAiDecisionModel>().AsSingle();
            Container.BindInterfacesExt<ShipAbilityFacade>();
            AudioShipData audioShipData = Container.Resolve<AudioShipData>();
            Container.Bind<IShipSfxView>().To<ShipSfxView>()
                .FromComponentInNewPrefab(audioShipData.ShipSfx.ViewPrefab)
                .UnderTransform(context => context.Container.ResolveId<Transform>(EntityBindType.ViewTransform))
                .AsSingle();
            Container.BindInterfacesTo<ShipSfxPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesExt<ShipOrderFacade>();
            Container.BindInterfacesExt<SelectionFacade>();
            Container.BindInterfacesExt<HealthFacade>();
            Container.BindInterfacesExt<HardPointsFacade>();
            Container.BindInterfacesExt<CombatModifiersFacade>();

            switch (_playerType)
            {
                case PlayerType.Player:
                    {
                        Container.BindInterfacesAndSelfTo<AudioDialogShipComponent>()
                            .FromComponentsInHierarchy()
                            .AsCached();


                        break;
                    }
                case PlayerType.Opponent:
                    {
                        Container.BindInterfacesAndSelfTo<FogVisibilityComponent>()
                            .FromComponentsInHierarchy()
                            .AsCached();
                        break;
                    }
            }
        }

        protected override void OnEntityCreated()
        {
            base.OnEntityCreated();
            Container.Install<EntityInstaller>(new object[] { Entity });
        }
    }
}
