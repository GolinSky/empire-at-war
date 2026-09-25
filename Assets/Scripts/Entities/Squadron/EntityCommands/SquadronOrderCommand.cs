using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.EntityCommands
{
    public sealed class SquadronOrderCommand : IMoveCommand, IAttackCommand, IAttackMoveCommand, IStopCommand,
        IGuardCommand, IWaypointMoveCommand, IHuntCommand, IRetreatCommand
    {
        private readonly Squadron _squadron;

        public SquadronOrderCommand(Squadron squadron) => _squadron = squadron;

        public Vector3 WorldPosition => _squadron.WorldPosition;
        public float NavigationRadius => _squadron.NavigationRadius;

        public void MoveTo(Vector2 screenPosition) => _squadron.MoveTo(screenPosition);
        public void MoveTo(Vector3 worldPosition) => _squadron.MoveTo(worldPosition);
        public void Attack(IEntity target, Vector3 formationOffset) => _squadron.Attack(target, formationOffset);
        public void AttackMoveTo(Vector3 worldPosition) => _squadron.AttackMoveTo(worldPosition);
        public void Stop() => _squadron.Stop();
        public void Guard(IEntity friendly, Vector3 offset) => _squadron.Guard(friendly, offset);
        public void MoveAlong(IReadOnlyList<Vector3> waypoints) => _squadron.MoveAlong(waypoints);
        public void Hunt() => _squadron.Hunt();
        public void Retreat(Vector3 destination) => _squadron.Retreat(destination);
    }
}
