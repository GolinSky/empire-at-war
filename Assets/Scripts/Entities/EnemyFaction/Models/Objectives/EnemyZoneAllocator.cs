using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    /// <summary>
    /// Pools every available ship and sends forces to zones in rank order. A zone gets ships only when together they
    /// beat its defenders, or when it is held, under attack and has own satellites standing (the ships fight around
    /// them); the centre force is led by the strongest free ship. A force already sent keeps its zone:
    /// it leaves on its own lost local fight, not on a zone recount. No ship idles: ships that complete no new force
    /// join the nearest force out, or, when no zone can be beaten, attack <see cref="WeakTarget"/> together.
    /// </summary>
    public sealed class EnemyZoneAllocator
    {
        public const int RESERVE = -1;

        private readonly EnemyZoneEvaluator _evaluator = new EnemyZoneEvaluator();
        private readonly ForceComposition _force = new ForceComposition();
        private readonly List<int> _order = new List<int>();
        private readonly List<int> _tiers = new List<int>();
        private readonly List<float> _scores = new List<float>();
        private readonly List<bool> _served = new List<bool>();
        private readonly List<int> _picked = new List<int>();
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
            for (int zone = 0; zone < zones.Count; zone++) _served.Add(false);

            // A force already sent keeps its zone; its own local fight decides when it pulls back.
            for (int ship = 0; ship < ships.Count; ship++)
            {
                int zone = current[ship];
                if (zone == RESERVE || zones[zone].IsDenied) continue;
                assignment[ship] = zone;
                _served[zone] = true;
            }

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
                _scores.Add(assessment.Score);
                if (!_served[zone] && !zones[zone].IsDenied) _order.Add(zone);
            }
            _order.Sort(_compareZones);

            foreach (int zone in _order)
            {
                _force.Clear();
                if (TryFill(zone, zones, ships, distance, assignment)) _served[zone] = true;
                else if (GatherZone == RESERVE) GatherZone = zone;
            }

            AssignLeftovers(zones, ships.Count, distance, assignment);
            SelectDeployZone(assignment);
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
        /// No waiting fleet: a ship that completed no force joins the nearest force out. With no force out it
        /// attacks the weak-phase target with every other leftover ship.
        /// </summary>
        private void AssignLeftovers(IReadOnlyList<EnemyZoneObjective> zones, int shipCount,
            Func<int, int, float> distance, List<int> assignment)
        {
            bool anyServed = _served.Contains(true);
            int weak = WeakTarget >= 0 && WeakTarget < zones.Count && !zones[WeakTarget].IsDenied ? WeakTarget : RESERVE;
            for (int ship = 0; ship < shipCount; ship++)
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
                    if (candidate >= nearestDistance) continue;
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
        /// goes to fight around them.
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
                    if (!defendSatellites || _picked.Count == 0) return false;
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
