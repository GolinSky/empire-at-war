using EmpireAtWar.Entities.Units;
using System.Linq;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.EntityFacades;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Selection;
using UnityEngine;
using Zenject;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Ship
{
    public sealed class ShipInstaller : DynamicEntityInstaller<Ship, ShipData>
    {
        private ILocalPlayer _localPlayer;

        private string _shipDataPath;

        private ShipType _shipType;
        private PlayerId _owner;

        protected override string DataPath => _shipDataPath;
        protected override string PrefabPath => _shipType + base.PrefabPath;

        [Inject]
        public void Construct(ILocalPlayer localPlayer, ShipsData shipsData, ShipType shipType, PlayerId owner)
        {
            _localPlayer = localPlayer;
            _shipType = shipType;
            _owner = owner;
            _shipDataPath = shipsData.GetShipDataPath(shipType);
        }

        protected override void InstallFeatures(ShipData data)
        {
            Container.BindEntityExt(_owner);
            Container.BindEntityExt(_shipType);

            Container
                .BindSelectionFeature(SelectionType.Ship)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindWeaponFeature()
                .BindCombatModifiersFeature()
                .BindFogOfWarFeature(!_localPlayer.IsFriendly(_owner));

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
            if (data.Abilities.Contains(ShipAbilityId.CompositeBeam))
                Container.BindInterfacesExt<CompositeBeamFacade>();
            Container.BindInterfacesExt<ShipOrderFacade>();
            Container.BindInterfacesExt<ShipDestroyFacade>();
            Container.BindInterfacesExt<ShipTooltipFacade>();
            Container.BindInterfacesAndSelfTo<UnitTypeFacade>().AsSingle().WithArguments(UnitTypeId.Ship(_shipType));
        }

        private void BindAudio()
        {
            Container.Bind<IShipEngineAudioObserver>().To<ShipMoveModel>().FromResolve();
            Container.BindInterfacesAndSelfTo<AudioShipComponent>()
                .FromComponentsInHierarchy().AsCached();

            // Voice lines only play for the local player's own ships.
            if (!_localPlayer.IsLocal(_owner)) return;
            Container.BindInterfacesAndSelfTo<AudioDialogShipComponent>()
                .FromComponentsInHierarchy().AsCached();
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
            Container.BindInterfacesAndSelfTo<AbilityApproachState>().AsSingle();
            Container.Bind<UnitOrderModel>().AsSingle();
            Container.Bind<ShipOrderRunner>().AsSingle();
        }
    }
}
