using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    public sealed class UnitOrderService : IUnitOrderService
    {
        private readonly IEntityLocator _entityLocator;
        private readonly IReinforcementZonesSystem _zones;
        private readonly UnitOrderSettings _settings;

        public UnitOrderService(IEntityLocator entityLocator,
            IReinforcementZonesSystem zones, UnitOrderSettings settings)
        {
            _entityLocator = entityLocator;
            _zones = zones;
            _settings = settings;
        }

        public event Action<UnitOrder> OrderIssued;

        public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point)
        {
            List<IMoveFacade> commands = Collect<IMoveFacade>(receivers);
            List<Vector3> slots = Compact(commands, point,
                command => command.WorldPosition, command => command.NavigationRadius);
            for (int i = 0; i < commands.Count; i++) commands[i].MoveTo(slots[i]);
            Publish(UnitActionId.Move, receivers, commands.Count, point);
        }

        public void IssueMove(IReadOnlyList<IEntity> receivers,
            IReadOnlyList<Vector3> destinations)
        {
            if (receivers.Count != destinations.Count)
                throw new ArgumentException("One destination is required per receiver.");
            int issued = 0;
            for (int i = 0; i < receivers.Count; i++)
            {
                if (!IsAlive(receivers[i]) ||
                    !receivers[i].TryGetFacade(out IMoveFacade command)) continue;
                command.MoveTo(destinations[i]);
                issued++;
            }
            Publish(UnitActionId.Move, receivers, issued,
                destinations.Count > 0 ? destinations[0] : default);
        }

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target)
        {
            if (target == null || !IsAlive(target)) return;
            IssueAttack(receivers, target, AttackOffsets(receivers, target));
        }

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target,
            IReadOnlyList<Vector3> offsets)
        {
            IssueAttack(receivers, target, offsets, UnitOrderModel.NO_HARD_POINT);
        }

        public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId)
        {
            if (target == null || !IsAlive(target)) return;
            IssueAttack(receivers, target, AttackOffsets(receivers, target), hardPointId);
        }

        private List<Vector3> AttackOffsets(IReadOnlyList<IEntity> receivers, IEntity target)
        {
            List<IAttackFacade> commands = Collect<IAttackFacade>(receivers);
            Vector3 point = target.GetFacade<IEntityTransformFacade>().Transform.position;
            List<Vector3> slots = Compact(commands, point,
                command => command.WorldPosition, command => command.NavigationRadius);
            List<Vector3> offsets = new List<Vector3>(slots.Count);
            foreach (Vector3 slot in slots)
                offsets.Add(new Vector3(slot.x - point.x, 0f, slot.z - point.z));
            return offsets;
        }

        // Receivers without hardpoint targeting (squadrons) attack the whole ship.
        private void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target,
            IReadOnlyList<Vector3> offsets, int hardPointId)
        {
            if (target == null || !IsAlive(target)) return;
            bool targetsHardPoint = hardPointId != UnitOrderModel.NO_HARD_POINT;
            int movingIndex = 0;
            int issued = 0;
            foreach (IEntity receiver in receivers)
            {
                if (!IsAlive(receiver)) continue;
                if (targetsHardPoint && receiver.TryGetFacade(out IHardPointAttackFacade hardPointAttack))
                {
                    Vector3 offset = receiver.TryGetFacade(out IAttackFacade _)
                        ? offsets[movingIndex++]
                        : Vector3.zero;
                    hardPointAttack.AttackHardPoint(target, hardPointId, offset);
                    issued++;
                }
                else if (receiver.TryGetFacade(out IAttackFacade attack))
                {
                    attack.Attack(target, offsets[movingIndex++]);
                    issued++;
                }
                else if (receiver.TryGetFacade(out IFocusFireFacade focus))
                {
                    focus.FocusFire(target);
                    issued++;
                }
            }
            Publish(UnitActionId.Attack, receivers, issued,
                target.GetFacade<IEntityTransformFacade>().Transform.position, target,
                targetHardPointId: hardPointId);
        }

        public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point)
        {
            List<IAttackMoveFacade> commands = Collect<IAttackMoveFacade>(receivers);
            List<Vector3> slots = Compact(commands, point,
                command => command.WorldPosition, command => command.NavigationRadius);
            for (int i = 0; i < commands.Count; i++) commands[i].AttackMoveTo(slots[i]);
            Publish(UnitActionId.AttackMove, receivers, commands.Count, point);
        }

        public void IssueStop(IReadOnlyList<IEntity> receivers)
        {
            List<IStopFacade> commands = Collect<IStopFacade>(receivers);
            foreach (IStopFacade command in commands) command.Stop();
            Publish(UnitActionId.Stop, receivers, commands.Count, default);
        }

        public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly)
        {
            if (friendly == null || !IsAlive(friendly)) return;
            List<IGuardFacade> commands = new List<IGuardFacade>();
            foreach (IEntity receiver in receivers)
                if (IsAlive(receiver) && receiver.Id != friendly.Id &&
                    receiver.TryGetFacade(out IGuardFacade command)) commands.Add(command);
            Vector3 point = friendly.GetFacade<IEntityTransformFacade>().Transform.position;
            List<Vector3> slots = Compact(commands, point,
                command => command.WorldPosition, command => command.NavigationRadius);
            List<Vector3> offsets = new List<Vector3>(slots.Count);
            foreach (Vector3 slot in slots)
                offsets.Add(new Vector3(slot.x - point.x, 0f, slot.z - point.z));
            IssueGuard(receivers, friendly, offsets);
        }

        public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly,
            IReadOnlyList<Vector3> offsets)
        {
            if (friendly == null || !IsAlive(friendly)) return;
            int index = 0;
            foreach (IEntity receiver in receivers)
                if (IsAlive(receiver) && receiver.Id != friendly.Id &&
                    receiver.TryGetFacade(out IGuardFacade command))
                    command.Guard(friendly, offsets[index++]);
            Publish(UnitActionId.Guard, receivers, index,
                friendly.GetFacade<IEntityTransformFacade>().Transform.position, friendly);
        }

        public void IssueWaypointMove(IReadOnlyList<IEntity> receivers,
            IReadOnlyList<Vector3> waypoints)
        {
            if (waypoints.Count == 0) return;
            List<IWaypointMoveFacade> commands = Collect<IWaypointMoveFacade>(receivers);
            List<Vector3> slots = Compact(commands, waypoints[0],
                command => command.WorldPosition, command => command.NavigationRadius);
            for (int i = 0; i < commands.Count; i++)
            {
                Vector3 offset = slots[i] - waypoints[0];
                List<Vector3> route = new List<Vector3>(waypoints.Count);
                foreach (Vector3 waypoint in waypoints) route.Add(waypoint + offset);
                commands[i].MoveAlong(route);
            }
            Publish(UnitActionId.WaypointMove, receivers, commands.Count,
                waypoints[0], waypoints: new List<Vector3>(waypoints));
        }

        public void IssueHunt(IReadOnlyList<IEntity> receivers)
        {
            List<IHuntFacade> commands = Collect<IHuntFacade>(receivers);
            foreach (IHuntFacade command in commands) command.Hunt();
            Publish(UnitActionId.Hunt, receivers, commands.Count, default);
        }

        public void IssueRetreat(IReadOnlyList<IEntity> receivers)
        {
            List<IRetreatFacade> commands = Collect<IRetreatFacade>(receivers);
            if (commands.Count == 0) return;
            if (!TryGetRetreatPoint(receivers[0].PlayerType, out Vector3 point))
                throw new InvalidOperationException("No retreat destination exists for the receivers.");
            List<Vector3> slots = Compact(commands, point,
                command => command.WorldPosition, command => command.NavigationRadius);
            for (int i = 0; i < commands.Count; i++)
                commands[i].Retreat(slots[i]);
            Publish(UnitActionId.Retreat, receivers, commands.Count, point);
        }

        private bool TryGetRetreatPoint(EmpireAtWar.Models.Factions.PlayerType side,
            out Vector3 point)
        {
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.PlayerType != side ||
                    !(entity.Model is ISpaceStationModelObserver) || !IsAlive(entity)) continue;
                Vector3 station = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (!_zones.TryGetDefaultZoneCenter(side, out Vector3 zone))
                    throw new InvalidOperationException(
                        "The operational station has no default reinforcement zone.");
                Vector3 direction = zone - station;
                direction.y = 0f;
                point = station + direction.normalized * _settings.StationClearance;
                return true;
            }
            return _zones.TryGetDefaultZoneCenter(side, out point);
        }

        private static List<TCommand> Collect<TCommand>(IReadOnlyList<IEntity> receivers)
            where TCommand : IEntityFacade
        {
            List<TCommand> commands = new List<TCommand>();
            foreach (IEntity entity in receivers)
                if (IsAlive(entity) && entity.TryGetFacade(out TCommand command))
                    commands.Add(command);
            return commands;
        }

        private static bool IsAlive(IEntity entity) =>
            !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;

        private static List<Vector3> Compact<TCommand>(IReadOnlyList<TCommand> commands,
            Vector3 point, Func<TCommand, Vector3> position, Func<TCommand, float> radius)
        {
            List<FormationPoint> positions = new List<FormationPoint>(commands.Count);
            List<float> radii = new List<float>(commands.Count);
            foreach (TCommand command in commands)
            {
                Vector3 origin = position(command);
                positions.Add(new FormationPoint(origin.x, origin.z));
                radii.Add(radius(command));
            }
            List<FormationPoint> destinations = new List<FormationPoint>();
            FormationModel.CalculateCompactDestinations(positions, radii,
                new FormationPoint(point.x, point.z), destinations);
            List<Vector3> slots = new List<Vector3>(destinations.Count);
            for (int i = 0; i < destinations.Count; i++)
                slots.Add(new Vector3(destinations[i].X, position(commands[i]).y,
                    destinations[i].Z));
            return slots;
        }

        private void Publish(UnitActionId action, IReadOnlyList<IEntity> receivers,
            int count, Vector3 point, IEntity target = null,
            IReadOnlyList<Vector3> waypoints = null, int targetHardPointId = UnitOrderModel.NO_HARD_POINT)
        {
            if (count > 0)
                OrderIssued?.Invoke(new UnitOrder(action, receivers[0].PlayerType,
                    point, target, waypoints, targetHardPointId));
        }
    }
}
