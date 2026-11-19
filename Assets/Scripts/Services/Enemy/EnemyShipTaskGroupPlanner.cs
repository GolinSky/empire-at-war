using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Owns ship membership. A new ship joins the reserve and gets a zone in the same decision cycle:
    /// <see cref="EnemyZoneAllocator"/> sends forces from the pool to zones, one group per zone, sized to beat its
    /// defenders, and leaves no ship waiting. When nothing can be beaten the pool attacks the weakest outer zone; a
    /// force that retreats from it switches to another outer zone and reinforcements gather there.
    /// </summary>
    public sealed class EnemyShipTaskGroupPlanner
    {
        /// <summary>After a retreat the weak-phase attack avoids that zone this long and tries another.</summary>
        public const float WEAK_ZONE_SWITCH_SECONDS = 90f;

        private readonly EnemyStrategicContextBuilder _contextBuilder;
        private readonly IUnitOrderService _orders;
        private readonly List<EnemyShipTaskGroup> _groups = new List<EnemyShipTaskGroup>();
        private readonly List<EnemyShipTaskGroup> _zoneGroups = new List<EnemyShipTaskGroup>();
        private readonly HashSet<IShipEntity> _members = new HashSet<IShipEntity>();
        private readonly EnemyShipTaskGroup _reserve;
        private readonly EnemyStrategicContextBuilder _pool;
        private readonly EnemyZoneAllocator _allocator = new EnemyZoneAllocator();
        private readonly List<IShipEntity> _poolShips = new List<IShipEntity>();
        private readonly List<ForceComposition> _poolForces = new List<ForceComposition>();
        private readonly List<ForceComposition> _forceCache = new List<ForceComposition>();
        private readonly List<int> _current = new List<int>();
        private readonly List<int> _assignment = new List<int>();
        private readonly List<string> _zoneReservations = new List<string>();
        private readonly Func<int, int, float> _distance;
        private readonly ForceComposition _poolTotal = new ForceComposition();
        private string _retreatedZoneId;
        private float _retreatedUntil;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string _lastAllocation;
#endif

        public IReadOnlyList<EnemyShipTaskGroup> Groups => _groups;
        /// <summary>Where the reserve gathers; new ships are sent here. Null before the first allocation.</summary>
        public Vector3? GatherPoint { get; private set; }
        /// <summary>
        /// Centre of the zone new ships are needed at most (<see cref="EnemyZoneAllocator.DeployZone"/>); they arrive at
        /// the held relay or station nearest it. Null before the first allocation or when no zone needs ships.
        /// </summary>
        public Vector3? DeploymentPoint { get; private set; }
        /// <summary>Outer zones ships may take this cycle; production sizes capture wings from it.</summary>
        public int EligibleExpansionZoneCount { get; private set; }

        public EnemyShipTaskGroupPlanner(EnemyStrategicContextBuilder contextBuilder,
            EnemyStrategicDecisionModel decisionModel, EnemyTaskForceExecutor executor,
            IUnitOrderService orders)
        {
            _contextBuilder = contextBuilder;
            _contextBuilder.IsReserve = true;
            _orders = orders;
            _reserve = new EnemyShipTaskGroup(contextBuilder, decisionModel, executor) { IsMainForce = true };
            _pool = contextBuilder.CreateGroupBuilder();
            _distance = GetDistance;
            _groups.Add(_reserve);
        }

        public void Reconcile(IReadOnlyList<IShipEntity> ships, bool stationPressure = false)
        {
            _members.Clear();
            foreach (IShipEntity ship in ships) _members.Add(ship);
            _reserve.Ships.RemoveAll(ship => !_members.Contains(ship));
            foreach (EnemyShipTaskGroup group in _zoneGroups)
            {
                group.Ships.RemoveAll(ship => !_members.Contains(ship));
                // A station about to fall recalls every force.
                if (!stationPressure || !_contextBuilder.StationCritical) continue;
                _reserve.Ships.AddRange(group.Ships);
                group.Ships.Clear();
            }
            _zoneGroups.RemoveAll(group => group.Ships.Count == 0);

            _members.Clear();
            foreach (IShipEntity ship in _reserve.Ships) _members.Add(ship);
            foreach (EnemyShipTaskGroup group in _zoneGroups)
                foreach (IShipEntity ship in group.Ships) _members.Add(ship);
            // New ships join the reserve and gather with it; none flies to a fight alone.
            foreach (IShipEntity ship in ships)
                if (!_members.Contains(ship)) _reserve.Ships.Add(ship);
            RebuildGroups();
        }

        /// <summary>
        /// Sends forces from the pool to zones for the next decision cycle. Under station pressure no new force is
        /// sent: forces already out keep their zones or return, and the reserve defends.
        /// </summary>
        public void Allocate(IReadOnlyList<IShipEntity> ships, ISet<string> reservedObjectives, bool stationPressure,
            float now = 0f)
        {
            // New forces pause only while the station is about to fall.
            stationPressure &= _contextBuilder.StationCritical;
            NoteRetreats(now);
            if (CollectPool(ships))
            {
                Vector3 home = _pool.GetHome(_poolShips[0].WorldPosition);
                IReadOnlyList<EnemyZoneObjective> zones = _pool.CollectPoolZones(home, GetPoolSpeed());
                _current.Clear();
                foreach (IShipEntity ship in _poolShips)
                {
                    EnemyShipTaskGroup group = FindZoneGroup(ship);
                    // A retreating force is free again: the weak-phase loop sends it to another zone.
                    _current.Add(group == null || group.ContextBuilder.AssignedZoneId == _retreatedZoneId &&
                        now < _retreatedUntil ? EnemyZoneAllocator.RESERVE :
                        IndexOf(zones, group.ContextBuilder.AssignedZoneId));
                }
                bool behind = _pool.IsBehindOnZones();
                _allocator.StrongEconomy = _pool.HasStrongEconomy();
                _allocator.WeakTarget = SelectWeakTarget(zones, now);
                _allocator.Allocate(zones, _poolForces, _current, _distance, behind, _assignment);
                if (stationPressure)
                    for (int i = 0; i < _assignment.Count; i++)
                        if (_assignment[i] != _current[i]) _assignment[i] = EnemyZoneAllocator.RESERVE;
                for (int i = 0; i < _poolShips.Count; i++)
                {
                    IShipEntity ship = _poolShips[i];
                    EnemyShipTaskGroup target = _assignment[i] == EnemyZoneAllocator.RESERVE
                        ? _reserve : GetOrCreateZoneGroup(zones[_assignment[i]].Id);
                    if (target.Ships.Contains(ship)) continue;
                    _reserve.Ships.Remove(ship);
                    foreach (EnemyShipTaskGroup group in _zoneGroups) group.Ships.Remove(ship);
                    target.Ships.Add(ship);
                }
                _zoneGroups.RemoveAll(group => group.Ships.Count == 0);
                int gather = _allocator.GatherZone;
                // Weak phase: reinforcements gather near the zone the pool attacks.
                if (gather == EnemyZoneAllocator.RESERVE && _assignment.Contains(_allocator.WeakTarget))
                    gather = _allocator.WeakTarget;
                _contextBuilder.ReserveStagingPoint = gather == EnemyZoneAllocator.RESERVE
                    ? (Vector3?)null : _pool.GetPoolStagingPoint(gather, home);
                GatherPoint = _contextBuilder.ReserveStagingPoint ?? _pool.GetIdleGatherPoint(home);
                int deploy = _allocator.DeployZone;
                DeploymentPoint = deploy == EnemyZoneAllocator.RESERVE ? (Vector3?)null : _pool.GetPoolZoneCentre(deploy);
                EligibleExpansionZoneCount = 0;
                _zoneReservations.Clear();
                for (int zone = 0; zone < zones.Count; zone++)
                {
                    ReinforcementZonePresenter presenter = _pool.GetPoolZone(zone);
                    if (presenter == null) continue;
                    if (!presenter.IsMain && !zones[zone].IsDenied) EligibleExpansionZoneCount++;
                    if (_assignment.Contains(zone))
                        _zoneReservations.Add(_pool.GetExpansionReservationId(presenter));
                }
                RecordAllocation(zones, home, behind);
                RebuildGroups();
            }
            // Allied AIs see whole zones as taken, not only their individual sites.
            foreach (string reservation in _zoneReservations) reservedObjectives.Add(reservation);
        }

        /// <summary>A zone force that retreats marks its zone; the weak-phase attack switches to another one.</summary>
        private void NoteRetreats(float now)
        {
            foreach (EnemyShipTaskGroup group in _zoneGroups)
            {
                if (group.Context == null || group.Decision.State != EnemyStrategicState.RetreatValue) continue;
                _retreatedZoneId = group.ContextBuilder.AssignedZoneId;
                _retreatedUntil = now + WEAK_ZONE_SWITCH_SECONDS;
            }
        }

        /// <summary>
        /// Weak phase target: the outer zone the whole pool rates best against its defenders, never the zone a force
        /// just retreated from. The centre is never a weak-phase target.
        /// </summary>
        private int SelectWeakTarget(IReadOnlyList<EnemyZoneObjective> zones, float now)
        {
            _poolTotal.Clear();
            foreach (ForceComposition force in _poolForces) _poolTotal.Add(force);
            int best = EnemyZoneAllocator.RESERVE;
            float bestAdvantage = float.NegativeInfinity;
            for (int zone = 0; zone < zones.Count; zone++)
            {
                EnemyZoneObjective objective = zones[zone];
                if (objective.IsDenied || objective.IsCentre ||
                    objective.Id == _retreatedZoneId && now < _retreatedUntil) continue;
                float advantage = objective.Defenders.IsEmpty ? float.PositiveInfinity :
                    CombatMatchup.Advantage(_poolTotal, objective.Defenders);
                if (advantage <= bestAdvantage) continue;
                best = zone;
                bestAdvantage = advantage;
            }
            return best;
        }

        /// <summary>
        /// Ships the allocator may move: every zone force, and the reserve unless it defends the station or assaults
        /// the base. False when there is none.
        /// </summary>
        private bool CollectPool(IReadOnlyList<IShipEntity> ships)
        {
            _poolShips.Clear();
            _poolForces.Clear();
            bool reserveBusy = _reserve.Decision.State is EnemyStrategicState.AssaultBase or EnemyStrategicState.DefendBase;
            foreach (IShipEntity ship in ships)
            {
                if (_reserve.Ships.Contains(ship) && (reserveBusy || ship.CurrentOrder == UnitOrderType.Retreat))
                    continue;
                if (_forceCache.Count == _poolForces.Count) _forceCache.Add(new ForceComposition());
                ForceComposition force = _forceCache[_poolForces.Count];
                if (!_pool.TryBuildShipForce(ship, force)) continue;
                _poolShips.Add(ship);
                _poolForces.Add(force);
            }
            return _poolShips.Count > 0;
        }

        private float GetPoolSpeed()
        {
            float speed = float.PositiveInfinity;
            foreach (IShipEntity ship in _poolShips)
                if (ship.NavigationSpeed > 0f) speed = Mathf.Min(speed, ship.NavigationSpeed);
            return float.IsPositiveInfinity(speed) ? 1f : speed;
        }

        private float GetDistance(int ship, int zone)
        {
            Vector3 offset = _poolShips[ship].WorldPosition - _pool.GetPoolZoneCentre(zone);
            offset.y = 0f;
            return offset.magnitude;
        }

        private EnemyShipTaskGroup FindZoneGroup(IShipEntity ship)
        {
            foreach (EnemyShipTaskGroup group in _zoneGroups)
                if (group.Ships.Contains(ship)) return group;
            return null;
        }

        private EnemyShipTaskGroup GetOrCreateZoneGroup(string zoneId)
        {
            foreach (EnemyShipTaskGroup existing in _zoneGroups)
                if (existing.ContextBuilder.AssignedZoneId == zoneId) return existing;
            EnemyShipTaskGroup group = new EnemyShipTaskGroup(_contextBuilder.CreateGroupBuilder(),
                new EnemyStrategicDecisionModel(), new EnemyTaskForceExecutor(_orders), isCaptureDetachment: true);
            group.ContextBuilder.AssignedZoneId = zoneId;
            _zoneGroups.Add(group);
            return group;
        }

        private static int IndexOf(IReadOnlyList<EnemyZoneObjective> zones, string zoneId)
        {
            for (int i = 0; i < zones.Count; i++)
                if (zones[i].Id == zoneId) return i;
            return EnemyZoneAllocator.RESERVE;
        }

        // The reserve is listed first and evaluates first; an empty reserve is listed only when no force is out.
        private void RebuildGroups()
        {
            _groups.Clear();
            if (_reserve.Ships.Count > 0 || _zoneGroups.Count == 0) _groups.Add(_reserve);
            _groups.AddRange(_zoneGroups);
        }

        /// <summary>One <c>zoneAlloc</c> row whenever the split of ships over zones changes.</summary>
        private void RecordAllocation(IReadOnlyList<EnemyZoneObjective> zones, Vector3 home, bool behind)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var text = new System.Text.StringBuilder();
            int gather = _allocator.GatherZone;
            Vector3 staging = _contextBuilder.ReserveStagingPoint ?? home;
            text.Append(FormattableString.Invariant(
                $"pool={_poolShips.Count};reserve={_reserve.Ships.Count};gather={(gather == EnemyZoneAllocator.RESERVE ? "none" : zones[gather].Id)};staging=({staging.x:F0} {staging.z:F0});behind={behind};zones="));
            for (int zone = 0; zone < zones.Count; zone++)
            {
                int assigned = 0;
                foreach (int target in _assignment)
                    if (target == zone) assigned++;
                text.Append(FormattableString.Invariant(
                    $"{zones[zone].Id}:ships{assigned}:defenders{zones[zone].Defenders.UnitCount}:statics{zones[zone].StaticDefenders}{(zones[zone].IsDenied ? ":" + zones[zone].DenyReason : "")} "));
            }
            string allocation = text.ToString();
            if (allocation == _lastAllocation) return;
            _lastAllocation = allocation;
            _pool.RecordZoneAllocation(allocation);
#endif
        }

        internal static ReinforcementZonePresenter GetCentre(IReadOnlyList<ReinforcementZonePresenter> zones)
        {
            foreach (ReinforcementZonePresenter zone in zones)
                if (zone.IsMain) return zone;
            return null;
        }

        internal static ReinforcementZonePresenter GetNeighbour(IReadOnlyList<ReinforcementZonePresenter> zones,
            Vector3 home, ReinforcementZonePresenter exclude = null)
        {
            ReinforcementZonePresenter closest = null;
            float distance = float.PositiveInfinity;
            foreach (ReinforcementZonePresenter zone in zones)
            {
                if (!zone.IsCapturable || zone.IsMain || zone == exclude) continue;
                float candidate = DistanceSquared(home, zone.Center);
                if (candidate >= distance) continue;
                closest = zone;
                distance = candidate;
            }
            return closest;
        }

        internal static bool IsInZone(ReinforcementZonePresenter zone, Vector3 position) =>
            DistanceSquared(zone.Center, position) <= 4f * zone.Radius * zone.Radius;

        internal static Vector3 GetOpeningHome(AiWorldSnapshot world, PlayerId owner, Vector3 fallback)
        {
            foreach (AiWorldSnapshot.Entry station in world.Bases)
                if (station.Owner == owner) return station.Position;
            return fallback;
        }

        private static float DistanceSquared(Vector3 first, Vector3 second)
        {
            Vector3 offset = first - second;
            return offset.x * offset.x + offset.z * offset.z;
        }
    }
}
