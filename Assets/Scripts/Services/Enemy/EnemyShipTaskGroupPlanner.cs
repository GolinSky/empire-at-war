using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyShipTaskGroupPlanner
    {
        private readonly EnemyStrategicContextBuilder _contextBuilder;
        private readonly IUnitOrderService _orders;
        private readonly List<EnemyShipTaskGroup> _groups = new List<EnemyShipTaskGroup>();
        private readonly List<IShipEntity> _unassigned = new List<IShipEntity>();
        private readonly HashSet<IShipEntity> _members = new HashSet<IShipEntity>();
        private EnemyShipTaskGroup _captureGroup;
        private readonly EnemyShipTaskGroup _idleGroup;

        public IReadOnlyList<EnemyShipTaskGroup> Groups => _groups;

        public EnemyShipTaskGroupPlanner(EnemyStrategicContextBuilder contextBuilder,
            EnemyStrategicDecisionModel decisionModel, EnemyTaskForceExecutor executor,
            IUnitOrderService orders)
        {
            _contextBuilder = contextBuilder;
            _orders = orders;
            _idleGroup = new EnemyShipTaskGroup(contextBuilder, decisionModel, executor);
            _groups.Add(_idleGroup);
        }

        public void Reconcile(IReadOnlyList<IShipEntity> ships)
        {
            _members.Clear();
            foreach (IShipEntity ship in ships) _members.Add(ship);
            foreach (EnemyShipTaskGroup group in _groups)
                group.Ships.RemoveAll(ship => !_members.Contains(ship));
            if (_captureGroup != null && (ships.Count < 2 || _captureGroup.Ships.Count == 0 ||
                _captureGroup.Context != null && !_captureGroup.Context.Snapshot.HasCaptureTarget &&
                !_captureGroup.DecisionModel.IsRecovering))
            {
                _groups.Remove(_captureGroup);
                _captureGroup = null;
            }
            if (_captureGroup == null && ships.Count > 1) TryCreateCaptureGroup(ships);

            _members.Clear();
            foreach (EnemyShipTaskGroup group in _groups)
            {
                if (group.Ships.Count == 0) continue;
                // Keep travel companions stable until they are clearly outside the cohesion envelope.
                Vector3 anchor = group.Ships[0].WorldPosition;
                for (int i = group.Ships.Count - 1; i > 0; i--)
                    if (DistanceSquared(anchor, group.Ships[i].WorldPosition) >
                        4f * FleetCohesion.RADIUS * FleetCohesion.RADIUS)
                        group.Ships.RemoveAt(i);
                foreach (IShipEntity ship in group.Ships) _members.Add(ship);
            }
            foreach (IShipEntity ship in ships)
            {
                if (_members.Contains(ship)) continue;
                EnemyShipTaskGroup closest = null;
                float bestDistance = FleetCohesion.RADIUS * FleetCohesion.RADIUS;
                foreach (EnemyShipTaskGroup group in _groups)
                {
                    if (group.IsCaptureDetachment || group.DecisionModel.IsRecovering) continue;
                    float distance = group.Ships.Count == 0 ? bestDistance :
                        DistanceSquared(group.Ships[0].WorldPosition, ship.WorldPosition);
                    if (distance > bestDistance) continue;
                    closest = group;
                    bestDistance = distance;
                }
                if (closest == null)
                {
                    closest = CreateGroup(false);
                    _groups.Add(closest);
                }
                closest.Ships.Add(ship);
            }
            MergeNearbyGroups();
            _groups.RemoveAll(group => group.Ships.Count == 0);
            if (_groups.Count == 0) _groups.Add(_idleGroup);
        }

        private void TryCreateCaptureGroup(IReadOnlyList<IShipEntity> ships)
        {
            _unassigned.Clear();
            foreach (IShipEntity ship in ships)
                if (ship.CurrentOrder != UnitOrderType.Retreat && ship.CurrentOrder != UnitOrderType.Attack)
                    _unassigned.Add(ship);
            _unassigned.Sort((a, b) => b.NavigationSpeed.CompareTo(a.NavigationSpeed));
            EnemyShipTaskGroup candidate = CreateGroup(true);
            foreach (IShipEntity ship in _unassigned)
            {
                candidate.Ships.Clear();
                candidate.Ships.Add(ship);
                EnemyStrategicSnapshot snapshot = candidate.ContextBuilder.Build(candidate.Ships).Snapshot;
                if (!snapshot.HasCaptureTarget || snapshot.LocalAdvantage <=
                    EnemyAiDifficultyProfile.Get(snapshot.Difficulty).RetreatAdvantage) continue;
                foreach (EnemyShipTaskGroup group in _groups) group.Ships.Remove(ship);
                _captureGroup = candidate;
                _groups.Insert(0, candidate);
                return;
            }
        }

        private void MergeNearbyGroups()
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                EnemyShipTaskGroup first = _groups[i];
                if (first.IsCaptureDetachment || first.Ships.Count == 0) continue;
                for (int j = _groups.Count - 1; j > i; j--)
                {
                    EnemyShipTaskGroup second = _groups[j];
                    if (second.IsCaptureDetachment || second.Ships.Count == 0 ||
                        first.DecisionModel.IsRecovering != second.DecisionModel.IsRecovering ||
                        first.Decision.State != second.Decision.State) continue;
                    bool fits = true;
                    foreach (IShipEntity ship in second.Ships)
                        fits &= DistanceSquared(first.Ships[0].WorldPosition, ship.WorldPosition) <=
                            FleetCohesion.RADIUS * FleetCohesion.RADIUS;
                    if (!fits) continue;
                    first.Ships.AddRange(second.Ships);
                    _groups.RemoveAt(j);
                }
            }
        }

        private EnemyShipTaskGroup CreateGroup(bool capture) => new EnemyShipTaskGroup(
            _contextBuilder.CreateGroupBuilder(), new EnemyStrategicDecisionModel(),
            new EnemyTaskForceExecutor(_orders), capture);

        private static float DistanceSquared(Vector3 first, Vector3 second)
        {
            Vector3 offset = first - second;
            return offset.x * offset.x + offset.z * offset.z;
        }
    }
}
