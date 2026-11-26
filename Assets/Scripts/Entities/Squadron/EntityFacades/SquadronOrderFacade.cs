using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SquadronOrderFacade : IUnitOrderObserverFacade, IMoveFacade, IAttackFacade, IAttackMoveFacade, IStopFacade,
        IGuardFacade, IWaypointMoveFacade, IHuntFacade, IRetreatFacade, IShipAbilityCastFacade, IUnitOrderReplicaFacade
    {
        private readonly Squadron _squadron;

        public UnitOrderType CurrentOrder => _squadron.CurrentOrder;
        public IEntity AttackTarget => _squadron.AttackTarget;
        // Squadrons swarm what their radar finds; only ships break off an attack-move for a warship (W66).
        public bool IsEngagingEnRoute => false;
        public int OrderRevision => _squadron.OrderRevision;

        public void ApplyReplicaOrder(UnitOrderType order, bool isNewOrder) =>
            _squadron.ApplyReplicaOrder(order, isNewOrder);

        public Vector3 WorldPosition => _squadron.WorldPosition;
        public float NavigationRadius => _squadron.NavigationRadius;
        public float PaceSpeed => _squadron.PaceSpeed;
        public int HullTier => 0;
        public bool IsBackRow => false;

        public SquadronOrderFacade(Squadron squadron) => _squadron = squadron;

        public void MoveTo(Vector2 screenPosition) => _squadron.MoveTo(screenPosition);

        public void MoveTo(Vector3 worldPosition, PaceCluster pace = null) => _squadron.MoveTo(worldPosition, pace);

        public void Attack(IEntity target, Vector3 formationOffset, PaceCluster pace = null) =>
            _squadron.Attack(target, formationOffset, pace);

        // Fighters swarm whatever their own radar finds, so squadrons do not join the shared engagement.
        public void AttackMoveTo(Vector3 worldPosition, AttackMoveEngagement engagement, PaceCluster pace = null,
            FormationRow row = FormationRow.Front) =>
            _squadron.AttackMoveTo(worldPosition, pace);

        public void Stop() => _squadron.Stop();

        public void Guard(IEntity friendly, Vector3 offset) => _squadron.Guard(friendly, offset);

        public void MoveAlong(IReadOnlyList<Vector3> waypoints) => _squadron.MoveAlong(waypoints);

        public void Hunt() => _squadron.Hunt();

        public void Retreat(Vector3 destination) => _squadron.Retreat(destination);

        public void CastAbility(ShipAbilityId id, IEntity target, float range) =>
            _squadron.CastAbility(id, target, range);
    }
}
