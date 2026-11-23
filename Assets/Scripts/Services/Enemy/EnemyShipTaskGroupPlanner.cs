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
        /// <summary>
        /// After a retreat or a stall the zone is blocked this long: no ship is sent there and the weak-phase attack
        /// tries another zone. A held zone under attack is never blocked (A2).
        /// </summary>
        public const float WEAK_ZONE_SWITCH_SECONDS = 90f;
        /// <summary>
        /// A zone force kept below its attack line (its verdict is not Go) this long is a stalled siege: it is released
        /// and its zone blocked like a retreat. A held zone under attack is never released this way.
        /// </summary>
        public const float STALLED_SIEGE_SECONDS = 90f;
        /// <summary>After an own station is lost no new force leaves this long: ships gather at home in waves.</summary>
        public const float TURTLE_SECONDS = 180f;
        /// <summary>The weak-phase attack needs the whole pool to reach this share of the target's defenders.</summary>
        public const float WEAK_ATTACK_FLOOR = 0.6f;
        /// <summary>
        /// A zone force at a zone where no site changed hands this long is stalled by result. A cleared zone or one with
        /// no open site has nothing left to take: its garrison is never released this way (A5).
        /// </summary>
        public const float STALL_NO_FLIP_SECONDS = 120f;

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
        private readonly ForceComposition _groupForce = new ForceComposition();
        private readonly EnemyZoneEvaluator _evaluator = new EnemyZoneEvaluator();
        private readonly Dictionary<string, float> _belowEvenSince = new Dictionary<string, float>();
        private readonly List<string> _staleSieges = new List<string>();
        // Per zone with a force: its open sites and hold at the last check, and since when no site turned to the team.
        private readonly Dictionary<string, (int OpenSites, bool Held, float Since)> _flipWatch =
            new Dictionary<string, (int, bool, float)>();
        // W49: a zone group emptied for a cycle keeps its builder, decision model and executor memory for its zone.
        private readonly Dictionary<string, EnemyShipTaskGroup> _idleZoneGroups = new Dictionary<string, EnemyShipTaskGroup>();
        private readonly EnemyLastStandLatch _lastStand = new EnemyLastStandLatch();
        private readonly EnemyZoneScoutGate _scoutGate = new EnemyZoneScoutGate();
        private int _stationCount = -1;
        private float _turtleUntil = float.NegativeInfinity;
        // A3: every zone a force retreated or stalled from, with the time it is blocked until.
        private readonly Dictionary<string, float> _blockedUntil = new Dictionary<string, float>();
        private readonly HashSet<string> _blockedZoneIds = new HashSet<string>();
        private readonly List<string> _expiredBlocks = new List<string>();
        private string _lastAllocation;

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
        /// <summary>W48: the weak fleet gathers at one team station; every force is recalled and new ships land there.</summary>
        public bool IsLastStand => _lastStand.IsActive;
        /// <summary>W48: where the last stand gathers; null outside a last stand or with no living team station.</summary>
        public Vector3? LastStandPoint { get; private set; }
        /// <summary>W68: centre of a zone a force waits to attack until a scout has seen its defenders; null when none.</summary>
        public Vector3? ScoutTarget { get; private set; }
        /// <summary>
        /// W68: <see cref="DeploymentPoint"/> when its zone is rated (<see cref="EnemyZoneScoutGate.IsRated"/>) and still
        /// to be taken: finished ships land there as one planned wave. Null otherwise and during the opening.
        /// </summary>
        public Vector3? PlannedWaveTarget { get; private set; }

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
            ParkEmptyZoneGroups();

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
            // New forces pause only while the station is about to fall, or for a while after one was lost.
            stationPressure &= _contextBuilder.StationCritical;
            bool turtle = NoteStationLoss(now);
            bool lastStand = NoteLastStand(ships, turtle, now);
            if (LastStandPoint.HasValue) GatherPoint = LastStandPoint;
            bool opening = !_contextBuilder.HoldsOuterZone();
            ScoutTarget = null;
            PlannedWaveTarget = null;
            NoteRetreats(now);
            PruneBlocks(now);
            if (CollectPool(ships))
            {
                Vector3 home = _pool.GetHome(_poolShips[0].WorldPosition);
                IReadOnlyList<EnemyZoneObjective> zones = _pool.CollectPoolZones(home, GetPoolSpeed(), GetPoolCentre());
                for (int zone = 0; zone < zones.Count; zone++)
                    if (_pool.IsPoolZoneVisible(zone)) _scoutGate.NoteSeen(zones[zone].Id, now);
                NoteStalledSieges(zones, now);
                _current.Clear();
                foreach (IShipEntity ship in _poolShips)
                {
                    EnemyShipTaskGroup group = FindZoneGroup(ship);
                    // A retreating or stalled force is free again: the weak-phase loop sends it to another zone.
                    _current.Add(group == null || _blockedZoneIds.Contains(group.ContextBuilder.AssignedZoneId)
                        ? EnemyZoneAllocator.RESERVE : IndexOf(zones, group.ContextBuilder.AssignedZoneId));
                }
                bool behind = _pool.IsBehindOnZones();
                _allocator.StrongEconomy = _pool.HasStrongEconomy();
                _allocator.LeanZones = _contextBuilder.LateGameSituation;
                _allocator.BlockedZoneIds = _blockedZoneIds;
                _allocator.WeakTarget = SelectWeakTarget(zones);
                _allocator.Allocate(zones, _poolForces, _current, _distance, behind, _assignment);
                if (stationPressure)
                    for (int i = 0; i < _assignment.Count; i++)
                        if (_assignment[i] != _current[i]) _assignment[i] = EnemyZoneAllocator.RESERVE;
                // A lost station or a last stand: every force comes home and the fleet gathers before it moves again.
                if (turtle || lastStand)
                {
                    for (int i = 0; i < _assignment.Count; i++) _assignment[i] = EnemyZoneAllocator.RESERVE;
                    _pool.ClearAllyZoneStrike();
                }
                else
                {
                    ApplyZonePlans(zones, home, opening);
                    if (!opening && !stationPressure) ScoutBeforeCommit(zones, now);
                }
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
                ParkEmptyZoneGroups();
                SetZoneGuardPoints(zones, home);
                int gather = _allocator.GatherZone;
                // Weak phase: reinforcements gather near the zone the pool attacks.
                if (gather == EnemyZoneAllocator.RESERVE && _assignment.Contains(_allocator.WeakTarget))
                    gather = _allocator.WeakTarget;
                _contextBuilder.ReserveStagingPoint = gather == EnemyZoneAllocator.RESERVE || turtle || lastStand
                    ? (Vector3?)null : _pool.GetPoolStagingPoint(gather, home);
                GatherPoint = LastStandPoint ?? _contextBuilder.ReserveStagingPoint ?? _pool.GetIdleGatherPoint(home);
                int deploy = turtle || lastStand ? EnemyZoneAllocator.RESERVE : _allocator.DeployZone;
                DeploymentPoint = deploy == EnemyZoneAllocator.RESERVE ? (Vector3?)null : _pool.GetPoolZoneCentre(deploy);
                // W68: a rated zone still to be taken gets its reinforcements as one wave dropped beside it.
                if (!opening && deploy != EnemyZoneAllocator.RESERVE && !zones[deploy].IsHeld &&
                    _scoutGate.IsRated(zones[deploy].Id, false, now))
                    PlannedWaveTarget = DeploymentPoint;
                EligibleExpansionZoneCount = 0;
                _zoneReservations.Clear();
                _reserve.Turtle = turtle;
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

        /// <summary>
        /// True for <see cref="TURTLE_SECONDS"/> after the count of own stations drops: the AI holds its fleet at home
        /// instead of sending what is left out piece by piece.
        /// </summary>
        private bool NoteStationLoss(float now)
        {
            int stations = _contextBuilder.CountOwnStations();
            if (_stationCount >= 0 && stations < _stationCount) _turtleUntil = now + TURTLE_SECONDS;
            _stationCount = stations;
            return now < _turtleUntil;
        }

        /// <summary>
        /// W48: the main force's smoothed fleet advantage below <see cref="EnemyLastStandLatch.ADVANTAGE"/> while a team
        /// station is pressed, an own station was lost (<paramref name="turtle"/>) or an armada threatens a team station
        /// starts a last stand held at least <see cref="EnemyLastStandLatch.SECONDS"/>: every ship joins the reserve,
        /// which gathers beside the team station with the most platforms.
        /// </summary>
        private bool NoteLastStand(IReadOnlyList<IShipEntity> ships, bool turtle, float now)
        {
            bool threatened = turtle || _contextBuilder.TeamStationPressure ||
                _contextBuilder.IsArmadaThreateningTeamStation();
            bool active = _lastStand.Update(GetMainFleetAdvantage(), threatened, now);
            LastStandPoint = active && _contextBuilder.TryGetLastStandPoint(GetCentre(ships), out Vector3 point)
                ? point : (Vector3?)null;
            _contextBuilder.LastStandPoint = LastStandPoint;
            _reserve.LastStand = active;
            return active;
        }

        /// <summary>Smoothed fleet advantage of the largest rated group, the reserve first on a tie; NaN before any rating.</summary>
        private float GetMainFleetAdvantage()
        {
            EnemyShipTaskGroup main = null;
            foreach (EnemyShipTaskGroup group in _groups)
                if (group.Context != null && (main == null || group.Ships.Count > main.Ships.Count)) main = group;
            return main == null ? float.NaN : main.Context.Snapshot.FleetAdvantage;
        }

        private static Vector3 GetCentre(IReadOnlyList<IShipEntity> ships)
        {
            if (ships.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (IShipEntity ship in ships) sum += ship.WorldPosition;
            return sum / ships.Count;
        }

        /// <summary>
        /// W68 scout first (<see cref="EnemyZoneScoutGate"/>): ships newly sent to a zone with stale defenders wait while
        /// a scouting wing looks (<see cref="ScoutTarget"/>); after the wing head start the fastest of them goes alone as
        /// the scout; once the zone is seen, or the wait ran out, the force commits.
        /// </summary>
        private void ScoutBeforeCommit(IReadOnlyList<EnemyZoneObjective> zones, float now)
        {
            for (int zone = 0; zone < zones.Count; zone++)
            {
                if (!IsNewlyAssigned(zone))
                {
                    _scoutGate.Release(zones[zone].Id);
                    continue;
                }
                EnemyScoutStep step = _scoutGate.Step(zones[zone].Id, zones[zone].IsHeld, _current.Contains(zone), now);
                if (step == EnemyScoutStep.Commit) continue;
                ScoutTarget ??= _pool.GetPoolZoneCentre(zone);
                int scout = step == EnemyScoutStep.ShipScouts ? FindFastestNewlyAssigned(zone) : EnemyZoneAllocator.RESERVE;
                for (int i = 0; i < _assignment.Count; i++)
                    if (_assignment[i] == zone && _current[i] != zone && i != scout) _assignment[i] = _current[i];
            }
        }

        /// <summary>
        /// W47 zone plans after the allocator: a RaidAndLeave zone that is taken and clear of armed hostiles lets every
        /// ship go (no garrison). After the opening, the first AllyCombined zone with ships sent to it asks the team for
        /// a combined strike (allies gather at its hold-back point); newly sent ships wait until allied ships can reach
        /// the zone, then the request turns committed and both attack. No such zone clears the request.
        /// </summary>
        private void ApplyZonePlans(IReadOnlyList<EnemyZoneObjective> zones, Vector3 home, bool opening)
        {
            bool requested = false;
            for (int zone = 0; zone < zones.Count; zone++)
            {
                EnemyZonePlan plan = _allocator.GetPlan(zone);
                if (EnemyZoneTaskRules.LeavesZone(plan, zones[zone]))
                {
                    for (int i = 0; i < _assignment.Count; i++)
                        if (_assignment[i] == zone) _assignment[i] = EnemyZoneAllocator.RESERVE;
                    continue;
                }
                if (opening || plan != EnemyZonePlan.AllyCombined || zones[zone].IsOwned || !_assignment.Contains(zone))
                    continue;
                Vector3 centre = _pool.GetPoolZoneCentre(zone);
                bool alliesNear = _pool.CanAlliesReach(centre);
                if (!requested)
                {
                    _pool.RequestAllyZoneStrike(zones[zone].Id, centre, _pool.GetPoolHoldBackPoint(zone, home),
                        zones[zone].Defenders, committed: alliesNear);
                    requested = true;
                }
                if (!EnemyZoneTaskRules.WaitsForAllies(plan, alliesNear)) continue;
                for (int i = 0; i < _assignment.Count; i++)
                    if (_assignment[i] == zone && _current[i] != zone) _assignment[i] = _current[i];
            }
            if (!requested) _pool.ClearAllyZoneStrike();
        }

        /// <summary>
        /// W47: each zone force gets the point it keeps once its zone has no site left to take: the zone centre
        /// (garrison, dwell, whole force while hostiles approach), or the hold-back point while a HoldBackAndCounter zone
        /// is owned and quiet.
        /// </summary>
        private void SetZoneGuardPoints(IReadOnlyList<EnemyZoneObjective> zones, Vector3 home)
        {
            foreach (EnemyShipTaskGroup group in _zoneGroups)
            {
                EnemyStrategicContextBuilder builder = group.ContextBuilder;
                int zone = IndexOf(zones, builder.AssignedZoneId);
                if (zone == EnemyZoneAllocator.RESERVE)
                {
                    builder.ZoneGuardPoint = null;
                    builder.ZoneHoldsBack = false;
                    continue;
                }
                EnemyZonePlan plan = _allocator.GetPlan(zone);
                builder.ZoneHoldsBack = EnemyZoneTaskRules.HoldsBack(plan, zones[zone]);
                builder.ZoneGuardPoint = EnemyZoneTaskRules.GetGuardPoint(plan, zones[zone],
                    _pool.GetPoolZoneCentre(zone), _pool.GetPoolHoldBackPoint(zone, home));
            }
        }

        private bool IsNewlyAssigned(int zone)
        {
            for (int i = 0; i < _assignment.Count; i++)
                if (_assignment[i] == zone && _current[i] != zone) return true;
            return false;
        }

        private int FindFastestNewlyAssigned(int zone)
        {
            int fastest = EnemyZoneAllocator.RESERVE;
            for (int i = 0; i < _assignment.Count; i++)
                if (_assignment[i] == zone && _current[i] != zone && (fastest == EnemyZoneAllocator.RESERVE ||
                    _poolShips[i].NavigationSpeed > _poolShips[fastest].NavigationSpeed)) fastest = i;
            return fastest;
        }

        /// <summary>A zone force that retreats blocks its zone; the weak-phase attack switches to another one.</summary>
        private void NoteRetreats(float now)
        {
            foreach (EnemyShipTaskGroup group in _zoneGroups)
            {
                if (group.Context == null || group.Decision.State != EnemyStrategicState.RetreatValue) continue;
                Block(group.ContextBuilder.AssignedZoneId, now);
            }
        }

        private void Block(string zoneId, float now)
        {
            _blockedUntil[zoneId] = now + WEAK_ZONE_SWITCH_SECONDS;
            _blockedZoneIds.Add(zoneId);
        }

        /// <summary>A3: drops expired blocks and refreshes the set of blocked zone ids.</summary>
        private void PruneBlocks(float now)
        {
            _expiredBlocks.Clear();
            foreach (KeyValuePair<string, float> block in _blockedUntil)
                if (now >= block.Value) _expiredBlocks.Add(block.Key);
            foreach (string zoneId in _expiredBlocks) _blockedUntil.Remove(zoneId);
            _blockedZoneIds.Clear();
            foreach (string zoneId in _blockedUntil.Keys) _blockedZoneIds.Add(zoneId);
        }

        /// <summary>
        /// A zone force whose verdict is not Go for <see cref="STALLED_SIEGE_SECONDS"/> takes nothing and only waits to be
        /// caught: its zone is blocked like a retreat, so the ships are free again and the weak-phase attack switches to
        /// another zone. A1: the clock restarts whenever the force is a Go, the same line the late-game trim keeps.
        /// </summary>
        private void NoteStalledSieges(IReadOnlyList<EnemyZoneObjective> zones, float now)
        {
            // A zone no force holds starts its clock afresh when a force is sent again.
            _staleSieges.Clear();
            foreach (string zoneId in _belowEvenSince.Keys)
                if (FindZoneGroup(zoneId) == null) _staleSieges.Add(zoneId);
            foreach (string zoneId in _staleSieges) _belowEvenSince.Remove(zoneId);
            _staleSieges.Clear();
            foreach (string zoneId in _flipWatch.Keys)
                if (FindZoneGroup(zoneId) == null) _staleSieges.Add(zoneId);
            foreach (string zoneId in _staleSieges) _flipWatch.Remove(zoneId);
            foreach (EnemyShipTaskGroup group in _zoneGroups)
            {
                string zoneId = group.ContextBuilder.AssignedZoneId;
                int zone = IndexOf(zones, zoneId);
                if (zone == EnemyZoneAllocator.RESERVE || zones[zone].IsHeldUnderAttack)
                {
                    _belowEvenSince.Remove(zoneId);
                    _flipWatch.Remove(zoneId);
                    continue;
                }
                if (IsStalledByResult(zoneId, zones[zone], now))
                {
                    _belowEvenSince.Remove(zoneId);
                    _flipWatch.Remove(zoneId);
                    Block(zoneId, now);
                    _pool.RecordZoneAllocation(FormattableString.Invariant(
                        $"stall=noFlip;zone={zoneId};seconds={STALL_NO_FLIP_SECONDS:F0}"));
                    continue;
                }
                _groupForce.Clear();
                for (int i = 0; i < _poolShips.Count; i++)
                    if (group.Ships.Contains(_poolShips[i])) _groupForce.Add(_poolForces[i]);
                if (_evaluator.Assess(zones[zone], _groupForce, EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict ==
                    EnemyZoneVerdict.Go)
                {
                    _belowEvenSince.Remove(zoneId);
                    continue;
                }
                if (!_belowEvenSince.TryGetValue(zoneId, out float since))
                {
                    _belowEvenSince[zoneId] = now;
                    continue;
                }
                if (now - since < STALLED_SIEGE_SECONDS) continue;
                _belowEvenSince.Remove(zoneId);
                Block(zoneId, now);
            }
        }

        /// <summary>
        /// A zone force stalls by result when no site in its zone turned to the team for
        /// <see cref="STALL_NO_FLIP_SECONDS"/>. Destroyed or rebuilt structures are no progress. A cleared zone or one
        /// with no open site has nothing to take: its garrison never stalls this way (A5).
        /// </summary>
        private bool IsStalledByResult(string zoneId, EnemyZoneObjective zone, float now)
        {
            if (EnemyZoneAllocator.IsCleared(zone) || zone.OpenSites == 0)
            {
                _flipWatch.Remove(zoneId);
                return false;
            }
            bool watched = _flipWatch.TryGetValue(zoneId, out var watch);
            bool progress = !watched || IsStallProgress(watch.OpenSites, watch.Held, zone.OpenSites, zone.IsHeld);
            _flipWatch[zoneId] = (zone.OpenSites, zone.IsHeld, progress ? now : watch.Since);
            return !progress && now - watch.Since >= STALL_NO_FLIP_SECONDS;
        }

        /// <summary>A site turned to the team since the last check: fewer open sites, or the zone newly held.</summary>
        public static bool IsStallProgress(int previousOpenSites, bool previouslyHeld, int openSites, bool held) =>
            openSites < previousOpenSites || held && !previouslyHeld;

        private Vector3 GetPoolCentre()
        {
            Vector3 sum = Vector3.zero;
            foreach (IShipEntity ship in _poolShips) sum += ship.WorldPosition;
            return sum / _poolShips.Count;
        }

        /// <summary>
        /// Weak phase target: the outer zone the whole pool rates best against its defenders, never a blocked zone (a
        /// force retreated or stalled there), and only while the pool reaches <see cref="WEAK_ATTACK_FLOOR"/>. The
        /// centre is never a weak-phase target.
        /// </summary>
        private int SelectWeakTarget(IReadOnlyList<EnemyZoneObjective> zones)
        {
            _poolTotal.Clear();
            foreach (ForceComposition force in _poolForces) _poolTotal.Add(force);
            int best = EnemyZoneAllocator.RESERVE;
            float bestAdvantage = float.NegativeInfinity;
            for (int zone = 0; zone < zones.Count; zone++)
            {
                EnemyZoneObjective objective = zones[zone];
                if (objective.IsDenied || objective.IsCentre || _blockedZoneIds.Contains(objective.Id)) continue;
                float advantage = objective.Defenders.IsEmpty ? float.PositiveInfinity :
                    CombatMatchup.Advantage(_poolTotal, objective.Defenders);
                if (advantage <= bestAdvantage) continue;
                best = zone;
                bestAdvantage = advantage;
            }
            // A pool far too weak for every zone gathers instead of feeding the attack.
            return bestAdvantage >= WEAK_ATTACK_FLOOR ? best : EnemyZoneAllocator.RESERVE;
        }

        /// <summary>
        /// Ships the allocator may move: every zone force, and the reserve unless it defends the station, assaults the
        /// base or answers an allied station or a hostile armada. False when there is none.
        /// </summary>
        private bool CollectPool(IReadOnlyList<IShipEntity> ships)
        {
            _poolShips.Clear();
            _poolForces.Clear();
            bool reserveBusy = _reserve.Decision.State is EnemyStrategicState.AssaultBase or EnemyStrategicState.DefendBase ||
                _reserve.IsAnsweringTeam;
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
            if (_idleZoneGroups.Remove(zoneId, out EnemyShipTaskGroup parked))
            {
                _zoneGroups.Add(parked);
                return parked;
            }
            EnemyShipTaskGroup group = new EnemyShipTaskGroup(_contextBuilder.CreateGroupBuilder(),
                new EnemyStrategicDecisionModel(), new EnemyTaskForceExecutor(_orders), isCaptureDetachment: true);
            group.ContextBuilder.AssignedZoneId = zoneId;
            _zoneGroups.Add(group);
            return group;
        }

        private EnemyShipTaskGroup FindZoneGroup(string zoneId)
        {
            foreach (EnemyShipTaskGroup group in _zoneGroups)
                if (group.ContextBuilder.AssignedZoneId == zoneId) return group;
            return null;
        }

        private static int IndexOf(IReadOnlyList<EnemyZoneObjective> zones, string zoneId)
        {
            for (int i = 0; i < zones.Count; i++)
                if (zones[i].Id == zoneId) return i;
            return EnemyZoneAllocator.RESERVE;
        }

        /// <summary>
        /// W49: an emptied zone group leaves the active list but is kept per zone, so a force sent there again keeps
        /// its decision state, retreat point and order memory instead of starting from nothing.
        /// </summary>
        private void ParkEmptyZoneGroups()
        {
            foreach (EnemyShipTaskGroup group in _zoneGroups)
                if (group.Ships.Count == 0) _idleZoneGroups[group.ContextBuilder.AssignedZoneId] = group;
            _zoneGroups.RemoveAll(group => group.Ships.Count == 0);
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
            var text = new System.Text.StringBuilder();
            int gather = _allocator.GatherZone;
            Vector3 staging = _contextBuilder.ReserveStagingPoint ?? home;
            text.Append(FormattableString.Invariant(
                $"pool={_poolShips.Count};reserve={_reserve.Ships.Count};gather={(gather == EnemyZoneAllocator.RESERVE ? "none" : zones[gather].Id)};staging=({staging.x:F0} {staging.z:F0});behind={behind};lastStand={IsLastStand};scout={(ScoutTarget.HasValue ? FormattableString.Invariant($"({ScoutTarget.Value.x:F0} {ScoutTarget.Value.z:F0})") : "none")};plannedWave={PlannedWaveTarget.HasValue};zones="));
            for (int zone = 0; zone < zones.Count; zone++)
            {
                int assigned = 0;
                foreach (int target in _assignment)
                    if (target == zone) assigned++;
                text.Append(FormattableString.Invariant(
                    $"{zones[zone].Id}:ships{assigned}:defenders{zones[zone].Defenders.UnitCount}:statics{zones[zone].StaticDefenders}:plan={_allocator.GetPlan(zone)}:garrison={_allocator.GetGarrison(zone)}{(zones[zone].IsDenied ? ":" + zones[zone].DenyReason : "")} "));
            }
            string allocation = text.ToString();
            if (allocation == _lastAllocation) return;
            _lastAllocation = allocation;
            _pool.RecordZoneAllocation(allocation);
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
