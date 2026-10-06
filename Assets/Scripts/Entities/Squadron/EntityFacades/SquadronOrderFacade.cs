using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SquadronOrderFacade : IUnitOrderObserverFacade, IMoveFacade, IAttackFacade, IAttackMoveFacade, IStopFacade,
        IGuardFacade, IWaypointMoveFacade, IHuntFacade, IRetreatFacade, IShipAbilityCastFacade
    {
        private readonly Squadron _squadron;

        public UnitOrderType CurrentOrder => _squadron.CurrentOrder;

        public Vector3 WorldPosition => _squadron.WorldPosition;
        public float NavigationRadius => _squadron.NavigationRadius;

        public SquadronOrderFacade(Squadron squadron) => _squadron = squadron;

        public void MoveTo(Vector2 screenPosition) => _squadron.MoveTo(screenPosition);

        public void MoveTo(Vector3 worldPosition) => _squadron.MoveTo(worldPosition);

        public void Attack(IEntity target, Vector3 formationOffset) => _squadron.Attack(target, formationOffset);

        // Fighters swarm whatever their own radar finds, so squadrons do not join the shared engagement.
        public void AttackMoveTo(Vector3 worldPosition, AttackMoveEngagement engagement) =>
            _squadron.AttackMoveTo(worldPosition);

        public void Stop() => _squadron.Stop();

        public void Guard(IEntity friendly, Vector3 offset) => _squadron.Guard(friendly, offset);

        public void MoveAlong(IReadOnlyList<Vector3> waypoints) => _squadron.MoveAlong(waypoints);

        public void Hunt() => _squadron.Hunt();

        public void Retreat(Vector3 destination) => _squadron.Retreat(destination);

        public void CastAbility(ShipAbilityId id, IEntity target, float range) =>
            _squadron.CastAbility(id, target, range);
    }
}
