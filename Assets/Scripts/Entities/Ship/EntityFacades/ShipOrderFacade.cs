using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class ShipOrderFacade : IMoveFacade, IAttackFacade, IHardPointAttackFacade,
        IAttackMoveFacade, IStopFacade, IGuardFacade, IWaypointMoveFacade,
        IHuntFacade, IRetreatFacade, IUnitOrderObserverFacade, IAbilityFacingFacade,
        IShipAbilityCastFacade, INavigationFootprintFacade, IShipArrivalFacade, IUnitOrderReplicaFacade
    {
        private readonly IShipMovement _movement;
        private readonly ShipMoveModel _moveModel;
        private readonly IShipData _data;

        private readonly ShipOrderRunner _orders;

        public Vector3 WorldPosition => _movement.CurrentPosition;
        public float NavigationRadius => _movement.NavigationRadius;
        public float PaceSpeed => _movement.PaceSpeed;
        public int HullTier => _data.ShipClass.HullTier();
        // BattleCruisers and larger lead whatever their loadout (the Xyston is laser-heavy).
        public bool IsBackRow => (_data.Roles & (UnitRole.Artillery | UnitRole.AntiStrikecraft)) != 0 &&
            HullTier < ShipClass.BattleCruiser.HullTier();
        public bool IsArriving => _moveModel.Phase == MovementPhase.Arriving;
        public bool IsAbilityFacing => _orders.IsAbilityFacing;
        public void BeginAbilityFacing() => _orders.BeginAbilityFacing();
        public void FaceAbility(Vector3 direction) => _orders.FaceAbility(direction);
        public void EndAbilityFacing() => _orders.EndAbilityFacing();
        public UnitOrderType CurrentOrder => _orders.CurrentOrder;
        public IEntity AttackTarget => _orders.AttackTarget;
        public bool IsEngagingEnRoute => _orders.IsEngagingEnRoute;
        public int OrderRevision => _orders.OrderRevision;

        public void ApplyReplicaOrder(UnitOrderType order, bool isNewOrder) =>
            _orders.ApplyReplicaOrder(order, isNewOrder);

        public ShipOrderFacade(IShipMovement movement, ShipOrderRunner orders, ShipMoveModel moveModel, IShipData data)
        {
            _orders = orders;
            _movement = movement;
            _moveModel = moveModel;
            _data = data;
        }

        public void MoveTo(Vector2 screenPosition) => _orders.MoveTo(screenPosition);

        public void MoveTo(Vector3 worldPosition, PaceCluster pace = null) => _orders.MoveTo(worldPosition, pace);

        public void Attack(IEntity target, Vector3 formationOffset, PaceCluster pace = null) =>
            _orders.Attack(target, formationOffset, pace);

        public void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset, PaceCluster pace = null) =>
            _orders.AttackHardPoint(target, hardPointId, formationOffset, pace);

        public void AttackMoveTo(Vector3 worldPosition, AttackMoveEngagement engagement, PaceCluster pace = null,
            FormationRow row = FormationRow.Front) =>
            _orders.AttackMoveTo(worldPosition, engagement, pace, row);

        public void Stop() => _orders.Stop();

        public void Guard(IEntity friendly, Vector3 offset) => _orders.Guard(friendly, offset);

        public void MoveAlong(IReadOnlyList<Vector3> waypoints) =>
            _orders.MoveAlong(waypoints);

        public void Hunt() => _orders.Hunt();

        public void Retreat(Vector3 destination) => _orders.Retreat(destination);

        public void CastAbility(ShipAbilityId id, IEntity target, float range) =>
            _orders.CastAbility(id, target, range);
    }
}
