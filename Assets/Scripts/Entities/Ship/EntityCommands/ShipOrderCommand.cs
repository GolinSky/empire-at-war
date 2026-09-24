using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityCommands
{
    public sealed class ShipOrderCommand : IMoveCommand, IAttackCommand,
        IAttackMoveCommand, IStopCommand, IGuardCommand, IWaypointMoveCommand,
        IHuntCommand, IRetreatCommand
    {
        private readonly EmpireAtWar.Ship.Ship _ship;

        public ShipOrderCommand(EmpireAtWar.Ship.Ship ship) => _ship = ship;

        public Vector3 WorldPosition => _ship.WorldPosition;
        public float NavigationRadius => _ship.NavigationRadius;

        public void MoveTo(Vector2 screenPosition) => _ship.MoveTo(screenPosition);
        public void MoveTo(Vector3 worldPosition) => _ship.MoveTo(worldPosition);
        public void Attack(IEntity target, Vector3 formationOffset) =>
            _ship.Attack(target, formationOffset);
        public void AttackMoveTo(Vector3 worldPosition) =>
            _ship.AttackMoveTo(worldPosition);
        public void Stop() => _ship.Stop();
        public void Guard(IEntity friendly, Vector3 offset) => _ship.Guard(friendly, offset);
        public void MoveAlong(IReadOnlyList<Vector3> waypoints) =>
            _ship.MoveAlong(waypoints);
        public void Hunt() => _ship.Hunt();
        public void Retreat(Vector3 destination) => _ship.Retreat(destination);
    }
}
