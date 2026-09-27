using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Selection;
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
        private string _shipDataPath;

        protected override string DataPath => _shipDataPath;
        protected override string PrefabPath => _shipType + base.PrefabPath;

        [Inject]
        public void Construct(ShipType shipType, PlayerType playerType, ShipsData shipsData)
        {
            _shipType = shipType;
            _playerType = playerType;
            _shipDataPath = shipsData.GetShipDataPath(shipType);
        }

        protected override void InstallFeatures(ShipData data)
        {
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_shipType);

            Container
                .BindSelectionFeature(SelectionType.Ship)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindWeaponFeature()
                .BindCombatModifiersFeature()
                .BindFogOfWarFeature(_playerType);

            Container.Bind<ShipMoveModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipMoveComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            if (data.HangarBays.Count > 0)
            {
                Container.Bind<HangarModel>().AsSingle();
                Container.BindInterfacesAndSelfTo<HangarComponent>()
                    .FromComponentInHierarchy()
                    .AsCached();
            }

            BindAudio();
            BindOrders();

            Container.BindInterfacesExt<ShipAbilityFacade>();
            Container.BindInterfacesExt<ShipOrderFacade>();
        }

        private void BindAudio()
        {
            AudioShipData audioShipData = Repository.Load<AudioShipData>(nameof(AudioShipData));
            Container.Bind<AudioShipData>()
                .FromNewScriptableObject(audioShipData)
                .AsSingle();
            Container.Bind<ILateDisposable>().To<AudioShipData>().FromResolve();
            Container.Bind<AudioShipModel>().AsSingle();
            Container.Bind<IAudioShipModelObserver>().To<AudioShipModel>().FromResolve();
            Container.BindInterfacesAndSelfTo<AudioShipComponent>()
                .FromComponentsInHierarchy()
                .AsCached();

            Container.Bind<IShipSfxView>().To<ShipSfxView>()
                .FromComponentInNewPrefab(audioShipData.ShipSfx.ViewPrefab)
                .UnderTransform(context => context.Container.ResolveId<Transform>(EntityBindType.ViewTransform))
                .AsSingle();
            Container.BindInterfacesTo<ShipSfxPresenter>().AsSingle().NonLazy();

            if (_playerType != PlayerType.Player)
                return;

            Container.Bind<AudioShipDialogData>()
                .FromNewScriptableObject(Repository.Load<AudioShipDialogData>(nameof(AudioShipDialogData)))
                .AsSingle();
            Container.Bind<AudioShipDialogModel>().AsSingle();
            Container.Bind<IAudioShipDialogModelObserver>().To<AudioShipDialogModel>().FromResolve();
            Container.BindInterfacesAndSelfTo<AudioDialogShipComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
        }

        private void BindOrders()
        {
            Container.Bind<ShipStateMachine>().AsSingle();
            Container.BindInterfacesAndSelfTo<AttackTargetState>().AsSingle();
            Container.BindInterfacesAndSelfTo<IdleState>().AsSingle();
            Container.BindInterfacesAndSelfTo<NavigateState>().AsSingle();
            Container.BindInterfacesAndSelfTo<AttackMoveState>().AsSingle();
            Container.BindInterfacesAndSelfTo<GuardState>().AsSingle();
            Container.BindInterfacesAndSelfTo<HuntState>().AsSingle();
            Container.BindInterfacesAndSelfTo<FleeState>().AsSingle();
            Container.Bind<UnitOrderModel>().AsSingle();
            Container.Bind<ShipAIBrain>().AsSingle();
            Container.Bind<ShipOrderRunner>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShipAiDecisionModel>().AsSingle();
        }
    }
}
