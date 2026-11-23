using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    /// <summary>
    /// Pools every available ship and sends forces to zones in rank order. A zone gets ships only when together they
    /// beat its defenders, or when it is held, under attack and has own satellites standing (the ships fight around
    /// them while force and support reach <see cref="DEFEND_FLOOR"/>); the centre force is led by the strongest free
    /// ship. A force already sent keeps its zone: it leaves on its own lost local fight, not on a zone recount. Ships
    /// that complete no new force join the nearest force out only where they keep or make it a winning fight; with
    /// no force out they attack <see cref="WeakTarget"/> together. Otherwise they gather in the reserve: no ship is
    /// fed alone into a losing fight. A captured zone with no armed hostile left (<see cref="IsCleared"/>) keeps its
    /// whole capturing force while hostiles approach and for <see cref="GARRISON_DWELL_SECONDS"/>, then only its
    /// garrison: the fewest ships that, with the zone's support, outlast the threat until help arrives (W47). In the
    /// late-game situation (<see cref="LeanZones"/>) every other zone force keeps only what it needs: the rest stays
    /// with the main force. Zones rank by score minus the travel of the nearest free ship (W49).
    /// </summary>
    public sealed class EnemyZoneAllocator
    {
        public const int RESERVE = -1;
        /// <summary>
        /// A held zone under attack gets ships below an even fight only while they and the zone's own support reach
        /// this share of its defenders; below it they would only die one by one.
        /// </summary>
        public const float DEFEND_FLOOR = 0.6f;
        /// <summary>
        /// A captured zone keeps its whole capturing force this long (W49 dwell): site construction starts and
        /// reinforcements can arrive before ships leave for the next zone.
        /// </summary>
        public const float GARRISON_DWELL_SECONDS = 60f;
        /// <summary>The garrison must outlast the threat this multiple of the time help needs to arrive.</summary>
        public const float GARRISON_SURVIVAL_SCALE = 1.2f;
        /// <summary>Ranking cost per travel-distance unit of the nearest free ship; scores are in the same units.</summary>
        public const float TRAVEL_COST_SCALE = 1f;

        private readonly EnemyZoneEvaluator _evaluator = new EnemyZoneEvaluator();
        private readonly ForceComposition _force = new ForceComposition();
        private readonly List<int> _order = new List<int>();
        private readonly List<int> _tiers = new List<int>();
        private readonly List<float> _scores = new List<float>();
        private readonly List<bool> _served = new List<bool>();
        private readonly List<int> _picked = new List<int>();
        private readonly List<EnemyZonePlan> _plans = new List<EnemyZonePlan>();
        private readonly List<int> _garrisons = new List<int>();
        private readonly ForceComposition _poolTotal = new ForceComposition();
        private readonly Comparison<int> _compareZones;

        public EnemyZoneAllocator() => _compareZones = CompareZones;

        /// <summary>
        /// Best zone that got no force this pass: reinforcements gather for it. <see cref="RESERVE"/> when every zone
        /// is served or denied.
        /// </summary>
        public int GatherZone { get; private set; } = RESERVE;

        /// <summary>
        /// The zone new ships are needed at most: the gather zone, else the best-ranked zone a force went to, else the
        /// weak-phase target. New ships arrive at the held anchor nearest it. <see cref="RESERVE"/> when none.
        /// </summary>
        public int DeployZone { get; private set; } = RESERVE;

        /// <summary>
        /// Weak-phase target: the zone every leftover ship attacks when no zone can be beaten and no force is out.
        /// <see cref="RESERVE"/> keeps them in the reserve.
        /// </summary>
        public int WeakTarget { get; set; } = RESERVE;

        /// <summary>An outer zone and the home mine are held: the centre is no longer the last choice when behind.</summary>
        public bool StrongEconomy { get; set; }

        /// <summary>
        /// Late-game situation (a team station under pressure or a hostile armada at a team asset): a zone force is the
        /// smallest group that still beats the zone, and leftover ships stay with the main force. A held zone under
        /// attack keeps its whole force.
        /// </summary>
        public bool LeanZones { get; set; }

        /// <summary>
        /// The capture is finished and no armed hostile is left (no unit, no platform): from now on the garrison rule
        /// decides who stays (W65).
        /// </summary>
        public static bool IsCleared(EnemyZoneObjective zone) => zone.IsOwned && HasNoArmedHostile(zone);

        private static bool HasNoArmedHostile(EnemyZoneObjective zone) =>
            !zone.IsThreatened && zone.Defenders.IsEmpty && zone.StaticDefenders == 0;

        /// <summary>
        /// The zone's plan after the last <see cref="Allocate"/>: its <see cref="EnemyZoneObjective.Plan"/>, or
        /// <see cref="EnemyZonePlan.RaidAndLeave"/> when the garrison it needs costs a larger share of the fleet than
        /// the zone is worth (<see cref="EnemyZoneObjective.WorthShare"/>). <see cref="EnemyZonePlan.HoldBackAndCounter"/>:
        /// the zone's ships wait at <c>EnemyStrategicContextBuilder.GetPoolHoldBackPoint</c>.
        /// </summary>
        public EnemyZonePlan GetPlan(int zone) => _plans[zone];

        /// <summary>
        /// Ships the garrison rule kept at the zone in the last <see cref="Allocate"/>; -1 when it did not apply (the zone
        /// is not cleared, hostiles approach, or the capture is younger than <see cref="GARRISON_DWELL_SECONDS"/>).
        /// </summary>
        public int GetGarrison(int zone) => _garrisons[zone];

        /// <param name="ships">One force per pooled ship.</param>
        /// <param name="current">Per ship, the zone index it is assigned to now, or <see cref="RESERVE"/>.</param>
        /// <param name="distance">Travel distance from a ship index to a zone index.</param>
        /// <param name="assignment">Receives one zone index or <see cref="RESERVE"/> per ship.</param>
        public void Allocate(IReadOnlyList<EnemyZoneObjective> zones, IReadOnlyList<ForceComposition> ships,
            IReadOnlyList<int> current, Func<int, int, float> distance, bool behindOnZones, List<int> assignment)
        {
            assignment.Clear();
            for (int i = 0; i < ships.Count; i++) assignment.Add(RESERVE);
            GatherZone = RESERVE;
            _served.Clear();
            _plans.Clear();
            _garrisons.Clear();
            for (int zone = 0; zone < zones.Count; zone++)
            {
                _served.Add(false);
                _plans.Add(zones[zone].Plan);
                _garrisons.Add(-1);
            }
            _poolTotal.Clear();
            for (int ship = 0; ship < ships.Count; ship++) _poolTotal.Add(ships[ship]);

            // A force already sent keeps its zone; its own local fight decides when it pulls back.
            for (int ship = 0; ship < ships.Count; ship++)
            {
                int zone = current[ship];
                if (zone == RESERVE || zones[zone].IsDenied) continue;
                assignment[ship] = zone;
                _served[zone] = true;
            }

            // A cleared zone keeps its garrison, and every other zone in the late-game situation only the force it needs.
            for (int zone = 0; zone < zones.Count; zone++)
                if (_served[zone]) _served[zone] = Trim(zone, zones, ships, distance, assignment);

            // A kept force below the commit line is topped up when the free ships can lift it back above.
            for (int zone = 0; zone < zones.Count; zone++)
            {
                if (!_served[zone]) continue;
                _force.Clear();
                for (int ship = 0; ship < ships.Count; ship++)
                    if (assignment[ship] == zone) _force.Add(ships[ship]);
                if (_evaluator.Assess(zones[zone], _force, EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict !=
                    EnemyZoneVerdict.Go) TryFill(zone, zones, ships, distance, assignment);
            }

            _force.Clear();
            for (int ship = 0; ship < ships.Count; ship++)
                if (assignment[ship] == RESERVE) _force.Add(ships[ship]);
            _order.Clear();
            _tiers.Clear();
            _scores.Clear();
            for (int zone = 0; zone < zones.Count; zone++)
            {
                EnemyZoneAssessment assessment = _evaluator.Assess(zones[zone], _force,
                    EnemyZoneEvaluator.COMMIT_ADVANTAGE);
                _tiers.Add(EnemyZoneEvaluator.GetTier(zones[zone], assessment.Verdict, behindOnZones, StrongEconomy));
                _scores.Add(assessment.Score -
                    TRAVEL_COST_SCALE * NearestFreeDistance(zone, ships.Count, distance, assignment));
                if (!_served[zone] && !zones[zone].IsDenied) _order.Add(zone);
            }
            _order.Sort(_compareZones);

            foreach (int zone in _order)
            {
                // Own wings take a zone with no armed hostile; ships go only where no wing holds it. A cleared zone is
                // taken: only the garrison rule keeps ships there, no new ship is sent.
                if (IsWingHeld(zones[zone]) || IsCleared(zones[zone])) continue;
                _force.Clear();
                if (TryFill(zone, zones, ships, distance, assignment)) _served[zone] = true;
                else if (GatherZone == RESERVE) GatherZone = zone;
            }

            AssignLeftovers(zones, ships, distance, assignment);
            SelectDeployZone(assignment);
        }

        // The zone's force with this ship added is a Go. A zone with no armed hostile, and every zone in the late-game
        // situation, takes a leftover only when its force is not yet a Go.
        private bool WinsWith(int zone, int ship, IReadOnlyList<EnemyZoneObjective> zones,
            IReadOnlyList<ForceComposition> ships, List<int> assignment)
        {
            _force.Clear();
            for (int other = 0; other < ships.Count; other++)
                if (assignment[other] == zone) _force.Add(ships[other]);
            if ((LeanZones || HasNoArmedHostile(zones[zone])) && IsGo(zones[zone], _force)) return false;
            _force.Add(ships[ship]);
            return IsGo(zones[zone], _force);
        }

        private bool IsGo(EnemyZoneObjective zone, ForceComposition force) =>
            _evaluator.Assess(zone, force, EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict == EnemyZoneVerdict.Go;

        /// <summary>
        /// Own wings stand in a zone with no armed hostile and beat what is left there without ships: the zone needs
        /// no ship.
        /// </summary>
        private bool IsWingHeld(EnemyZoneObjective zone)
        {
            if (!HasNoArmedHostile(zone) || !HasStrikecraft(zone.OwnSupport)) return false;
            _force.Clear();
            return IsGo(zone, _force);
        }

        private static bool HasStrikecraft(ForceComposition force)
        {
            foreach (ShipClass shipClass in ForceComposition.Classes)
                if (shipClass.IsStrikecraft() && force.Hull(shipClass) > 0f) return true;
            return false;
        }

        /// <summary>
        /// A cleared zone keeps its garrison (<see cref="KeepGarrison"/>) once no hostile approaches and the capture is
        /// <see cref="GARRISON_DWELL_SECONDS"/> old; before that the whole capturing force stays. In the late-game
        /// situation any other zone releases its ships farthest from it first while the rest still beats the zone
        /// (the last ship only when own wings hold it); a held zone under attack keeps its whole force. Returns whether
        /// the zone keeps ships.
        /// </summary>
        private bool Trim(int zone, IReadOnlyList<EnemyZoneObjective> zones, IReadOnlyList<ForceComposition> ships,
            Func<int, int, float> distance, List<int> assignment)
        {
            EnemyZoneObjective objective = zones[zone];
            if (IsCleared(objective))
            {
                if (!objective.IsHostileNear && objective.HeldSeconds >= GARRISON_DWELL_SECONDS)
                    KeepGarrison(zone, objective, ships, distance, assignment);
            }
            else if (LeanZones && !objective.IsHeldUnderAttack)
                while (true)
                {
                    int farthest = RESERVE;
                    float farthestDistance = float.NegativeInfinity;
                    int kept = 0;
                    for (int ship = 0; ship < ships.Count; ship++)
                    {
                        if (assignment[ship] != zone) continue;
                        kept++;
                        float candidate = distance(ship, zone);
                        if (candidate <= farthestDistance) continue;
                        farthest = ship;
                        farthestDistance = candidate;
                    }
                    if (farthest == RESERVE) break;
                    _force.Clear();
                    for (int ship = 0; ship < ships.Count; ship++)
                        if (assignment[ship] == zone && ship != farthest) _force.Add(ships[ship]);
                    bool stillWins = kept > 1 ? IsGo(objective, _force) : IsWingHeld(objective);
                    if (!stillWins) break;
                    assignment[farthest] = RESERVE;
                }
            for (int ship = 0; ship < ships.Count; ship++)
                if (assignment[ship] == zone) return true;
            return false;
        }

        /// <summary>
        /// Keeps the zone's nearest ships that, with its <see cref="EnemyZoneObjective.OwnSupport"/>, outlast its
        /// <see cref="EnemyZoneObjective.Threat"/> for <see cref="GARRISON_SURVIVAL_SCALE"/> × the time help needs, and
        /// releases the rest. Without such a threat no ship stays. A garrison that is a larger share of the fleet than
        /// the zone's <see cref="EnemyZoneObjective.WorthShare"/> is no garrison: the zone is raided and left (no token
        /// ship).
        /// </summary>
        private void KeepGarrison(int zone, EnemyZoneObjective objective, IReadOnlyList<ForceComposition> ships,
            Func<int, int, float> distance, List<int> assignment)
        {
            _picked.Clear();
            for (int ship = 0; ship < ships.Count; ship++)
                if (assignment[ship] == zone) _picked.Add(ship);
            // Nearest first: the ships already standing in the zone stay.
            for (int i = 1; i < _picked.Count; i++)
                for (int j = i; j > 0 && distance(_picked[j - 1], zone) > distance(_picked[j], zone); j--)
                    (_picked[j - 1], _picked[j]) = (_picked[j], _picked[j - 1]);
            int count = CountGarrison(objective, ships);
            if (count > 0 && !IsWorthGarrison(objective, ships, count))
            {
                count = 0;
                _plans[zone] = EnemyZonePlan.RaidAndLeave;
            }
            _garrisons[zone] = count;
            for (int i = count; i < _picked.Count; i++) assignment[_picked[i]] = RESERVE;
        }

        /// <summary>Fewest of <see cref="_picked"/> (in order) that outlast the threat; all of them when none suffice.</summary>
        private int CountGarrison(EnemyZoneObjective objective, IReadOnlyList<ForceComposition> ships)
        {
            if (objective.Threat.IsEmpty) return 0;
            float needed = GARRISON_SURVIVAL_SCALE * objective.HelpSeconds;
            _force.CopyFrom(objective.OwnSupport);
            if (CombatMatchup.TimeToDestroy(objective.Threat, _force) >= needed) return 0;
            for (int i = 0; i < _picked.Count; i++)
            {
                _force.Add(ships[_picked[i]]);
                if (CombatMatchup.TimeToDestroy(objective.Threat, _force) >= needed) return i + 1;
            }
            return _picked.Count;
        }

        // The garrison's share of the pool's strength (a force ratio) is no larger than the zone's share of the income.
        private bool IsWorthGarrison(EnemyZoneObjective objective, IReadOnlyList<ForceComposition> ships, int count)
        {
            _force.Clear();
            for (int i = 0; i < count; i++) _force.Add(ships[_picked[i]]);
            return Math.Min(1f, CombatMatchup.Advantage(_force, _poolTotal)) <= objective.WorthShare;
        }

        private static float NearestFreeDistance(int zone, int shipCount, Func<int, int, float> distance,
            List<int> assignment)
        {
            float nearest = float.PositiveInfinity;
            for (int ship = 0; ship < shipCount; ship++)
                if (assignment[ship] == RESERVE) nearest = Math.Min(nearest, distance(ship, zone));
            return float.IsPositiveInfinity(nearest) ? 0f : nearest;
        }

        private void SelectDeployZone(List<int> assignment)
        {
            DeployZone = GatherZone;
            if (DeployZone != RESERVE) return;
            for (int zone = 0; zone < _served.Count; zone++)
                if (_served[zone] && (DeployZone == RESERVE || CompareZones(zone, DeployZone) < 0)) DeployZone = zone;
            if (DeployZone == RESERVE && WeakTarget != RESERVE && assignment.Contains(WeakTarget)) DeployZone = WeakTarget;
        }

        /// <summary>
        /// A ship that completed no force joins the nearest force out whose fight it keeps or makes a Go; a losing
        /// force gets no single reinforcements. With no force out every leftover attacks the weak-phase target
        /// together. A ship with neither gathers in the reserve until a force can be formed.
        /// </summary>
        private void AssignLeftovers(IReadOnlyList<EnemyZoneObjective> zones, IReadOnlyList<ForceComposition> ships,
            Func<int, int, float> distance, List<int> assignment)
        {
            bool anyServed = _served.Contains(true);
            int weak = WeakTarget >= 0 && WeakTarget < zones.Count && !zones[WeakTarget].IsDenied ? WeakTarget : RESERVE;
            for (int ship = 0; ship < ships.Count; ship++)
            {
                if (assignment[ship] != RESERVE) continue;
                if (!anyServed)
                {
                    assignment[ship] = weak;
                    continue;
                }
                int nearest = RESERVE;
                float nearestDistance = float.PositiveInfinity;
                for (int zone = 0; zone < zones.Count; zone++)
                {
                    if (!_served[zone]) continue;
                    float candidate = distance(ship, zone);
                    if (candidate >= nearestDistance || !WinsWith(zone, ship, zones, ships, assignment)) continue;
                    nearest = zone;
                    nearestDistance = candidate;
                }
                assignment[ship] = nearest;
            }
        }

        /// <summary>
        /// Adds free ships to <see cref="_force"/> until it is a Go for the zone, then assigns them: the nearest
        /// first, except that the centre force is led by the strongest free ship. Assigns nothing when the free ships
        /// together are not enough, except at a held zone under attack with own satellites standing: every free ship
        /// goes to fight around them while together with the zone's support they reach <see cref="DEFEND_FLOOR"/>.
        /// </summary>
        private bool TryFill(int zone, IReadOnlyList<EnemyZoneObjective> zones, IReadOnlyList<ForceComposition> ships,
            Func<int, int, float> distance, List<int> assignment)
        {
            _picked.Clear();
            bool defendSatellites = zones[zone].IsHeldUnderAttack && zones[zone].OwnStructures > 0;
            while (true)
            {
                int next = _picked.Count == 0 && zones[zone].IsCentre
                    ? FindStrongestFree(ships, assignment)
                    : FindNearestFree(zone, ships.Count, distance, assignment);
                if (next == RESERVE)
                {
                    if (!defendSatellites || _picked.Count == 0 || _evaluator.Assess(zones[zone], _force,
                            EnemyZoneEvaluator.COMMIT_ADVANTAGE).Advantage < DEFEND_FLOOR) return false;
                    foreach (int ship in _picked) assignment[ship] = zone;
                    return true;
                }
                _picked.Add(next);
                _force.Add(ships[next]);
                if (_evaluator.Assess(zones[zone], _force, EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict !=
                    EnemyZoneVerdict.Go) continue;
                foreach (int ship in _picked) assignment[ship] = zone;
                return true;
            }
        }

        private int FindNearestFree(int zone, int shipCount, Func<int, int, float> distance, List<int> assignment)
        {
            int nearest = RESERVE;
            float nearestDistance = float.PositiveInfinity;
            for (int ship = 0; ship < shipCount; ship++)
            {
                if (assignment[ship] != RESERVE || _picked.Contains(ship)) continue;
                float candidate = distance(ship, zone);
                if (candidate >= nearestDistance) continue;
                nearest = ship;
                nearestDistance = candidate;
            }
            return nearest;
        }

        private int FindStrongestFree(IReadOnlyList<ForceComposition> ships, List<int> assignment)
        {
            int strongest = RESERVE;
            float best = float.NegativeInfinity;
            for (int ship = 0; ship < ships.Count; ship++)
            {
                if (assignment[ship] != RESERVE || _picked.Contains(ship)) continue;
                float durability = Durability(ships[ship]);
                if (durability <= best) continue;
                strongest = ship;
                best = durability;
            }
            return strongest;
        }

        // Hull plus shields over every class: the bigger and more expensive hull leads.
        private static float Durability(ForceComposition force)
        {
            float total = 0f;
            foreach (ShipClass shipClass in ForceComposition.Classes)
                total += force.Hull(shipClass) + force.Shields(shipClass);
            return total;
        }

        private int CompareZones(int left, int right)
        {
            int tier = _tiers[left].CompareTo(_tiers[right]);
            if (tier != 0) return tier;
            int score = _scores[right].CompareTo(_scores[left]);
            return score != 0 ? score : left.CompareTo(right);
        }
    }
}
