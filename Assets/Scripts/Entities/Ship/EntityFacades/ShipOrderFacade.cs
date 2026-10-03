using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Orders;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class ShipOrderFacade : IMoveFacade, IAttackFacade, IHardPointAttackFacade,
        IAttackMoveFacade, IStopFacade, IGuardFacade, IWaypointMoveFacade,
        IHuntFacade, IRetreatFacade, IUnitOrderObserverFacade
    {
        private readonly IShipMovement _movement;

        private readonly ShipOrderRunner _orders;

        public Vector3 WorldPosition => _movement.CurrentPosition;
        public float NavigationRadius => _movement.NavigationRadius;
        public EmpireAtWar.Entities.BaseEntity.Orders.UnitOrderType CurrentOrder => _orders.CurrentOrder;

        public ShipOrderFacade(IShipMovement movement, ShipOrderRunner orders)
        {
            _orders = orders;
            _movement = movement;
        }

        public void MoveTo(Vector2 screenPosition) => _orders.MoveTo(screenPosition);

        public void MoveTo(Vector3 worldPosition) => _orders.MoveTo(worldPosition);

        public void Attack(IEntity target, Vector3 formationOffset) =>
            _orders.Attack(target, formationOffset);

        public void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset) =>
            _orders.AttackHardPoint(target, hardPointId, formationOffset);

        public void AttackMoveTo(Vector3 worldPosition, AttackMoveEngagement engagement) =>
            _orders.AttackMoveTo(worldPosition, engagement);

        public void Stop() => _orders.Stop();

        public void Guard(IEntity friendly, Vector3 offset) => _orders.Guard(friendly, offset);

        public void MoveAlong(IReadOnlyList<Vector3> waypoints) =>
            _orders.MoveAlong(waypoints);

        public void Hunt() => _orders.Hunt();

        public void Retreat(Vector3 destination) => _orders.Retreat(destination);
    }
}
