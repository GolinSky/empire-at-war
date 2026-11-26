using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.UnitOrders
{
    public sealed class UnitOrderService : IUnitOrderService
    {
        private const float ATTACK_ARC_HALF_DEGREES = 60f;

        private readonly IEntityLocator _entityLocator;
        private readonly IReinforcementZonesSystem _zones;
        private readonly UnitOrderSettings _settings;
        private readonly IMapObstacleContactProvider _obstacles;
        private readonly PaceClusterMergeService _mergeService;
        private readonly IFocusFireSlots _focusFire;
        private readonly List<(IEntity Receiver, float DistanceSquared)> _slotCandidates =
            new List<(IEntity, float)>();
        private readonly HashSet<long> _overflowIds = new HashSet<long>();
        private readonly List<FormationPoint> _compactSlots = new List<FormationPoint>();
        private readonly List<RadarContact> _obstacleBuffer = new List<RadarContact>();
        private readonly List<PaceMemberCandidate> _paceCandidates = new List<PaceMemberCandidate>();
        private readonly List<PaceCluster> _createdClusters = new List<PaceCluster>();
        private readonly Func<System.Numerics.Vector2, System.Numerics.Vector2, bool> _isSegmentClear;
        private int _nextOrderId;

        public event Action<UnitOrder> OrderIssued;
        /// <summary>Every attack order (players and AI) with its receivers; the online host relays the target.</summary>
        public event Action<IReadOnlyList<IEntity>, IEntity, int> AttackIssued;

        public UnitOrderService(IEntityLocator entityLocator,
            IReinforcementZonesSystem zones, UnitOrderSettings settings,
            IMapObstacleContactProvider obstacles, PaceClusterMergeService mergeService, IFocusFireSlots focusFire)
        {
            _focusFire = focusFire;
            _entityLocator = entityLocator;
            _zones = zones;
            _settings = settings;
            _obstacles = obstacles;
            _mergeService = mergeService;
            _isSegmentClear = IsSegmentClear;
        }

        // Accepted host orders update client presentation without invoking any unit commands.
        public void PublishReplicaFeedback(UnitOrder order) => OrderIssued?.Invoke(order);

        public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation)
        {
            List<IEntity> entities = new List<IEntity>();
            List<IMoveFacade> commands = Collect<IMoveFacade>(receivers, entities);
            List<Vector3> slots = Compact(commands, point,
                command => command.WorldPosition, command => command.NavigationRadius);
            PaceCluster[] clusters = pace == MovePace.Formation
                ? CreateMoveClusters(entities, commands)
                : null;
            for (int i = 0; i < commands.Count; i++) commands[i].MoveTo(slots[i], clusters?[i]);
            Publish(UnitActionId.Move, receivers, commands.Count, point, isLoose: pace == MovePace.Loose);
        }

        public void IssueMove(IReadOnlyList<IEntity> receivers,
            IReadOnlyList<Vector3> destinations)
        {
            if (receivers.Count != destinations.Count)
                throw new ArgumentException("One destination is required per receiver.");
            List<IEntity> entities = new List<IEntity>();
            List<IMoveFacade> commands = new List<IMoveFacade>();
            List<Vector3> targets = new List<Vector3>();
            for (int i = 0; i < receivers.Count; i++)
            {
                if (!IsAlive(receivers[i]) ||
                    !receivers[i].TryGetFacade(out IMoveFacade command)) continue;
                entities.Add(receivers[i]);
                commands.Add(command);
                targets.Add(destinations[i]);
            }
            PaceCluster[] clusters = CreateMoveClusters(entities, commands);
            for (int i = 0; i < commands.Count; i++) commands[i].MoveTo(targets[i], clusters?[i]);
            Publish(UnitActionId.Move, receivers, commands.Count,
                destinations.Count > 0 ? destinations[0] : default);
        }

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, MovePace pace = MovePace.Formation)
        {
            IssueCappedAttack(receivers, target, UnitOrderModel.NO_HARD_POINT, pace);
        }

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target,
            IReadOnlyList<Vector3> offsets)
        {
            if (target == null || !IsAlive(target)) return;
            List<IEntity> overflow = new List<IEntity>();
            List<IEntity> attackers = SplitFocusFire(receivers, target, overflow);
            IssueAttack(attackers, target, AttackerOffsets(receivers, attackers, offsets),
                UnitOrderModel.NO_HARD_POINT, MovePace.Formation);
            IssueOverflow(overflow, target, MovePace.Formation);
        }

        public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId,
            MovePace pace = MovePace.Formation)
        {
            IssueCappedAttack(receivers, target, hardPointId, pace);
        }

        private void IssueCappedAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId,
            MovePace pace)
        {
            if (target == null || !IsAlive(target)) return;
            List<IEntity> overflow = new List<IEntity>();
            List<IEntity> attackers = SplitFocusFire(receivers, target, overflow);
            IssueAttack(attackers, target, AttackOffsets(attackers, target), hardPointId, pace);
            IssueOverflow(overflow, target, pace);
        }

        // Focus fire cap: ships and squadrons fill separate pools of the attacking team's free slots. Receivers that
        // already hold the target keep their slot, the nearest others take what is left, the rest is overflow.
        // Receivers that cannot move (stations) and dead receivers always stay attackers.
        private List<IEntity> SplitFocusFire(IReadOnlyList<IEntity> receivers, IEntity target, List<IEntity> overflow)
        {
            List<IEntity> attackers = new List<IEntity>(receivers.Count);
            if (receivers.Count == 0) return attackers;
            int freeShips = _focusFire.FreeSlots(receivers[0].Owner, target, isSquadron: false);
            int freeSquadrons = _focusFire.FreeSlots(receivers[0].Owner, target, isSquadron: true);
            if (freeShips == int.MaxValue && freeSquadrons == int.MaxValue)
            {
                attackers.AddRange(receivers);
                return attackers;
            }

            Vector3 point = target.GetFacade<IEntityTransformFacade>().Transform.position;
            _slotCandidates.Clear();
            foreach (IEntity receiver in receivers)
            {
                if (!IsAlive(receiver) || !receiver.TryGetFacade(out IAttackFacade attack) ||
                    _focusFire.Holds(receiver, target)) continue;
                Vector3 offset = attack.WorldPosition - point;
                offset.y = 0f;
                _slotCandidates.Add((receiver, offset.sqrMagnitude));
            }

            _slotCandidates.Sort((first, second) => first.DistanceSquared.CompareTo(second.DistanceSquared));
            _overflowIds.Clear();
            foreach ((IEntity receiver, float _) in _slotCandidates)
            {
                if (receiver.IsSquadron())
                {
                    if (freeSquadrons > 0) freeSquadrons--;
                    else _overflowIds.Add(receiver.Id);
                }
                else if (freeShips > 0) freeShips--;
                else _overflowIds.Add(receiver.Id);
            }

            foreach (IEntity receiver in receivers)
                (_overflowIds.Contains(receiver.Id) ? overflow : attackers).Add(receiver);
            return attackers;
        }

        // Explicit offsets are aligned to the receivers that move; overflow drops out of the alignment.
        private static IReadOnlyList<Vector3> AttackerOffsets(IReadOnlyList<IEntity> receivers,
            IReadOnlyList<IEntity> attackers, IReadOnlyList<Vector3> offsets)
        {
            if (attackers.Count == receivers.Count) return offsets;
            List<Vector3> kept = new List<Vector3>(attackers.Count);
            int movingIndex = 0;
            int attackerIndex = 0;
            foreach (IEntity receiver in receivers)
            {
                bool isAttacker = attackerIndex < attackers.Count && ReferenceEquals(attackers[attackerIndex], receiver);
                if (isAttacker) attackerIndex++;
                if (!IsAlive(receiver) || !receiver.TryGetFacade(out IAttackFacade _)) continue;
                if (isAttacker) kept.Add(offsets[movingIndex]);
                movingIndex++;
            }

            return kept;
        }

        // Overflow fights on around the target: attack-move engages other hostiles there and claims no slot.
        private void IssueOverflow(IReadOnlyList<IEntity> overflow, IEntity target, MovePace pace)
        {
            if (overflow.Count == 0) return;
            IssueAttackMove(overflow, target.GetFacade<IEntityTransformFacade>().Transform.position, pace);
        }

        // Only ships get a slot, and a slot only picks the side: the attack state approaches along its direction
        // to firing range. Neighbouring slots are spaced so the hulls clear each other even at hull contact with the
        // target, and the whole arc stays within ATTACK_ARC_HALF_DEGREES of the group's own side.
        private List<Vector3> AttackOffsets(IReadOnlyList<IEntity> receivers, IEntity target)
        {
            List<Vector3> offsets = new List<Vector3>();
            List<int> shipIndices = new List<int>();
            List<Vector3> shipPositions = new List<Vector3>();
            List<float> shipRadii = new List<float>();
            Vector3 center = Vector3.zero;
            float maximumRadius = 0f;
            foreach (IEntity receiver in receivers)
            {
                if (!IsAlive(receiver) || !receiver.TryGetFacade(out IAttackFacade command)) continue;
                if (receiver.TryGetFacade(out IHardPointAttackFacade _))
                {
                    shipIndices.Add(offsets.Count);
                    shipPositions.Add(command.WorldPosition);
                    shipRadii.Add(command.NavigationRadius);
                    center += command.WorldPosition;
                    maximumRadius = Mathf.Max(maximumRadius, command.NavigationRadius);
                }

                offsets.Add(Vector3.zero);
            }

            if (shipIndices.Count == 0) return offsets;
            center /= shipIndices.Count;
            Vector3 point = target.GetFacade<IEntityTransformFacade>().Transform.position;
            float heading = Mathf.Atan2(center.z - point.z, center.x - point.x);
            float[] sides = new float[shipIndices.Count];
            int[] order = new int[shipIndices.Count];
            for (int i = 0; i < sides.Length; i++)
            {
                order[i] = i;
                sides[i] = Mathf.DeltaAngle(heading * Mathf.Rad2Deg,
                    Mathf.Atan2(shipPositions[i].z - point.z, shipPositions[i].x - point.x) * Mathf.Rad2Deg);
            }

            // Slots follow the ships' current order around the target, so assignments do not cross.
            Array.Sort(order, (first, second) => sides[first].CompareTo(sides[second]));
            float targetRadius = target.TryGetFacade(out INavigationFootprintFacade footprint)
                ? footprint.NavigationRadius
                : 0f;
            float contact = targetRadius + maximumRadius;
            float[] angles = new float[order.Length];
            for (int slot = 1; slot < order.Length; slot++)
            {
                float clearance = shipRadii[order[slot - 1]] + shipRadii[order[slot]];
                float step = contact > 0f ? 2f * Mathf.Asin(Mathf.Min(1f, clearance / (2f * contact))) : 0f;
                angles[slot] = angles[slot - 1] + step;
            }

            float arc = angles[order.Length - 1];
            float halfArc = ATTACK_ARC_HALF_DEGREES * Mathf.Deg2Rad;
            float scale = arc > 2f * halfArc ? 2f * halfArc / arc : 1f;
            for (int slot = 0; slot < order.Length; slot++)
            {
                float angle = heading + (angles[slot] - arc * 0.5f) * scale;
                offsets[shipIndices[order[slot]]] = new Vector3(Mathf.Cos(angle) * maximumRadius, 0f,
                    Mathf.Sin(angle) * maximumRadius);
            }

            return offsets;
        }

        // Receivers without hardpoint targeting (squadrons) attack the whole ship. Receivers here are past the cap split.
        private void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target,
            IReadOnlyList<Vector3> offsets, int hardPointId, MovePace pace)
        {
            if (target == null || !IsAlive(target)) return;
            bool targetsHardPoint = hardPointId != UnitOrderModel.NO_HARD_POINT;
            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            PaceCluster[] clusters = pace == MovePace.Formation
                ? CreateAttackClusters(receivers, targetPosition)
                : null;
            int movingIndex = 0;
            int issued = 0;
            foreach (IEntity receiver in receivers)
            {
                if (!IsAlive(receiver)) continue;
                if (targetsHardPoint && receiver.TryGetFacade(out IHardPointAttackFacade hardPointAttack))
                {
                    // Stations cannot move: no slot and no pace.
                    bool moves = receiver.TryGetFacade(out IAttackFacade _);
                    PaceCluster cluster = moves ? clusters?[movingIndex] : null;
                    Vector3 offset = moves ? offsets[movingIndex++] : Vector3.zero;
                    hardPointAttack.AttackHardPoint(target, hardPointId, offset, cluster);
                    issued++;
                }
                else if (receiver.TryGetFacade(out IAttackFacade attack))
                {
                    PaceCluster cluster = clusters?[movingIndex];
                    attack.Attack(target, offsets[movingIndex++], cluster);
                    issued++;
                }
                else if (receiver.TryGetFacade(out IFocusFireFacade focus))
                {
                    focus.FocusFire(target);
                    issued++;
                }
            }
            Publish(UnitActionId.Attack, receivers, issued, targetPosition, target,
                targetHardPointId: hardPointId);
            if (issued > 0) AttackIssued?.Invoke(receivers, target, hardPointId);
        }

        private PaceCluster[] CreateMoveClusters(IReadOnlyList<IEntity> entities, IReadOnlyList<IMoveFacade> commands)
        {
            if (commands.Count < 2) return null;
            _paceCandidates.Clear();
            for (int i = 0; i < commands.Count; i++)
            {
                Vector3 pos = commands[i].WorldPosition;
                _paceCandidates.Add(new PaceMemberCandidate(
                    new System.Numerics.Vector2(pos.x, pos.z),
                    commands[i].NavigationRadius,
                    commands[i].PaceSpeed,
                    entities[i].IsSquadron()));
            }
            return BuildClusters();
        }

        private PaceCluster[] CreateAttackClusters(IReadOnlyList<IEntity> receivers, Vector3 target)
        {
            _paceCandidates.Clear();
            foreach (IEntity receiver in receivers)
            {
                if (!IsAlive(receiver) || !receiver.TryGetFacade(out IAttackFacade attack)) continue;
                Vector3 pos = attack.WorldPosition;
                _paceCandidates.Add(new PaceMemberCandidate(
                    new System.Numerics.Vector2(pos.x, pos.z),
                    attack.NavigationRadius,
                    attack.PaceSpeed,
                    receiver.IsSquadron()));
            }
            return BuildClusters(target);
        }

        private PaceCluster[] CreateAttackMoveClusters(IReadOnlyList<IEntity> entities,
            IReadOnlyList<IAttackMoveFacade> commands, Vector3 target)
        {
            if (commands.Count < 2) return null;
            _paceCandidates.Clear();
            for (int i = 0; i < commands.Count; i++)
            {
                Vector3 pos = commands[i].WorldPosition;
                _paceCandidates.Add(new PaceMemberCandidate(
                    new System.Numerics.Vector2(pos.x, pos.z),
                    commands[i].NavigationRadius,
                    commands[i].PaceSpeed,
                    entities[i].IsSquadron()));
            }
            return BuildClusters(target);
        }

        // A target point marks clusters already near it as unpaced (attack and attack-move orders).
        private PaceCluster[] BuildClusters(Vector3? target = null)
        {
            if (_paceCandidates.Count < 2) return null;
            _obstacleBuffer.Clear();
            _obstacles.CopyContacts(_obstacleBuffer);
            _createdClusters.Clear();
            int orderId = ++_nextOrderId;
            PaceCluster[] clusters = PaceClusterBuilder.Build(
                _paceCandidates,
                orderId,
                _settings.PaceTuning,
                _obstacleBuffer.Count > 0 ? _isSegmentClear : null,
                _createdClusters,
                target.HasValue
                    ? new System.Numerics.Vector2(target.Value.x, target.Value.z)
                    : (System.Numerics.Vector2?)null);
            if (_createdClusters.Count > 0) _mergeService.RegisterRange(_createdClusters);
            return clusters;
        }

        private bool IsSegmentClear(System.Numerics.Vector2 from, System.Numerics.Vector2 to) =>
            ShipAvoidancePlanner.IsSegmentClear(
                new Vector3(from.X, 0f, from.Y),
                new Vector3(to.X, 0f, to.Y),
                _obstacleBuffer, 0f, 0f, 0f);

        public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point,
            MovePace pace = MovePace.Formation)
        {
            List<IEntity> entities = new List<IEntity>();
            List<IAttackMoveFacade> commands = Collect<IAttackMoveFacade>(receivers, entities);
            List<FormationRow> rows = new List<FormationRow>(commands.Count);
            List<Vector3> slots = AttackMoveRows(commands, point, rows);
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            PaceCluster[] clusters = pace == MovePace.Formation
                ? CreateAttackMoveClusters(entities, commands, point)
                : null;
            for (int i = 0; i < commands.Count; i++)
                commands[i].AttackMoveTo(slots[i], engagement, clusters?[i], rows[i]);
            Publish(UnitActionId.AttackMove, receivers, commands.Count, point);
        }

        public void IssueAttackMove(IReadOnlyList<IEntity> receivers,
            IReadOnlyList<Vector3> destinations)
        {
            if (receivers.Count != destinations.Count)
                throw new ArgumentException("One destination is required per receiver.");
            AttackMoveEngagement engagement = new AttackMoveEngagement();
            List<IEntity> entities = new List<IEntity>();
            List<IAttackMoveFacade> commands = new List<IAttackMoveFacade>();
            List<Vector3> targets = new List<Vector3>();
            for (int i = 0; i < receivers.Count; i++)
            {
                if (!IsAlive(receivers[i]) ||
                    !receivers[i].TryGetFacade(out IAttackMoveFacade command)) continue;
                entities.Add(receivers[i]);
                commands.Add(command);
                targets.Add(destinations[i]);
            }
            Vector3 center = Vector3.zero;
            foreach (Vector3 destination in targets) center += destination;
            if (targets.Count > 0) center /= targets.Count;
            PaceCluster[] clusters = CreateAttackMoveClusters(entities, commands, center);
            for (int i = 0; i < commands.Count; i++) commands[i].AttackMoveTo(targets[i], engagement, clusters?[i]);
            Publish(UnitActionId.AttackMove, receivers, commands.Count,
                destinations.Count > 0 ? destinations[0] : default);
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
                command => command.WorldPosition, command => command.NavigationRadius,
                friendly.TryGetFacade(out INavigationFootprintFacade footprint) ? footprint.NavigationRadius : 0f);
            List<Vector3> offsets = new List<Vector3>(slots.Count);
            foreach (Vector3 slot in slots)
                offsets.Add(new Vector3(slot.x - point.x, 0f, slot.z - point.z));
            IssueGuard(receivers, friendly, offsets);
        }

        public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly,
            IReadOnlyList<Vector3> offsets)
        {
            if (friendly == null || !IsAlive(friendly)) return;
            if (friendly.TryGetFacade(out INavigationFootprintFacade footprint))
            {
                int slot = 0;
                foreach (IEntity receiver in receivers)
                {
                    if (!IsAlive(receiver) || receiver.Id == friendly.Id ||
                        !receiver.TryGetFacade(out IGuardFacade guard)) continue;
                    Vector3 offset = offsets[slot++];
                    float clearance = footprint.NavigationRadius + guard.NavigationRadius;
                    if (offset.x * offset.x + offset.z * offset.z >= clearance * clearance) continue;
                    IssueGuard(receivers, friendly);
                    return;
                }
            }
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
            if (!TryGetRetreatPoint(receivers[0].Owner, out Vector3 point))
                throw new InvalidOperationException(
                    "No retreat destination exists for the receivers.");
            List<Vector3> slots = Compact(commands, point,
                command => command.WorldPosition, command => command.NavigationRadius);
            for (int i = 0; i < commands.Count; i++) commands[i].Retreat(slots[i]);
            Publish(UnitActionId.Retreat, receivers, commands.Count, point);
        }

        public void IssueRetreat(IReadOnlyList<IEntity> receivers, Vector3 point)
        {
            List<IRetreatFacade> commands = Collect<IRetreatFacade>(receivers);
            if (commands.Count == 0) return;
            List<Vector3> slots = Compact(commands, point,
                command => command.WorldPosition, command => command.NavigationRadius);
            for (int i = 0; i < commands.Count; i++) commands[i].Retreat(slots[i]);
            Publish(UnitActionId.Retreat, receivers, commands.Count, point);
        }

        private bool TryGetRetreatPoint(PlayerId side,
            out Vector3 point)
        {
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.Owner != side ||
                    !entity.IsPlayerBase() || !IsAlive(entity)) continue;
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

        private static List<TCommand> Collect<TCommand>(IReadOnlyList<IEntity> receivers, List<IEntity> entities)
            where TCommand : IEntityFacade
        {
            List<TCommand> commands = new List<TCommand>();
            foreach (IEntity entity in receivers)
            {
                if (!IsAlive(entity) || !entity.TryGetFacade(out TCommand command)) continue;
                entities.Add(entity);
                commands.Add(command);
            }
            return commands;
        }

        private static bool IsAlive(IEntity entity) =>
            !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits;

        private List<Vector3> Compact<TCommand>(IReadOnlyList<TCommand> commands,
            Vector3 point, Func<TCommand, Vector3> position, Func<TCommand, float> radius,
            float occupiedRadius = 0f)
        {
            List<FormationPoint> positions = new List<FormationPoint>(commands.Count);
            List<float> radii = new List<float>(commands.Count);
            foreach (TCommand command in commands)
            {
                Vector3 origin = position(command);
                positions.Add(ToPoint(origin));
                radii.Add(radius(command));
            }
            List<FormationPoint> destinations = new List<FormationPoint>();
            FormationModel.CalculateCompactDestinations(positions, radii,
                ToPoint(point), destinations, occupiedRadius, _compactSlots);
            List<Vector3> slots = new List<Vector3>(destinations.Count);
            for (int i = 0; i < destinations.Count; i++)
                slots.Add(new Vector3(destinations[i].X, position(commands[i]).y,
                    destinations[i].Z));
            return slots;
        }

        private static List<Vector3> AttackMoveRows(IReadOnlyList<IAttackMoveFacade> commands, Vector3 point,
            List<FormationRow> rows)
        {
            List<FormationPoint> positions = new List<FormationPoint>(commands.Count);
            List<float> radii = new List<float>(commands.Count);
            List<int> tiers = new List<int>(commands.Count);
            List<bool> backRow = new List<bool>(commands.Count);
            foreach (IAttackMoveFacade command in commands)
            {
                positions.Add(ToPoint(command.WorldPosition));
                radii.Add(command.NavigationRadius);
                tiers.Add(command.HullTier);
                backRow.Add(command.IsBackRow);
            }
            List<FormationPoint> destinations = new List<FormationPoint>(commands.Count);
            FormationModel.CalculateRowDestinations(positions, radii, tiers, backRow, ToPoint(point),
                destinations, rows);
            List<Vector3> slots = new List<Vector3>(destinations.Count);
            for (int i = 0; i < destinations.Count; i++)
                slots.Add(new Vector3(destinations[i].X, commands[i].WorldPosition.y, destinations[i].Z));
            return slots;
        }

        private void Publish(UnitActionId action, IReadOnlyList<IEntity> receivers,
            int count, Vector3 point, IEntity target = null,
            IReadOnlyList<Vector3> waypoints = null, int targetHardPointId = UnitOrderModel.NO_HARD_POINT,
            bool isLoose = false)
        {
            if (count > 0)
                OrderIssued?.Invoke(new UnitOrder(action, receivers[0].Owner,
                    point, target, waypoints, targetHardPointId, isLoose));
        }
    }
}
