using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Spreads wings over capture targets. A target with known defenders (hostile units and armed structures in
    /// range) gets wings only when together they are at least an even match; otherwise every wing stays away.
    /// Wing groups choose inside their own zone first (<see cref="AssignInZone"/>); the rest are spread after.
    /// </summary>
    public sealed class EnemySquadronCaptureAssignments
    {
        /// <summary>Wing power over the defenders needed to go in: an even fight.</summary>
        private const float EVEN_FIGHT = 1f;

        private readonly Dictionary<Vector3, int> _targets = new Dictionary<Vector3, int>();
        private readonly Dictionary<Vector3, ForceComposition> _defenders = new Dictionary<Vector3, ForceComposition>();
        private readonly List<ForceComposition> _defenderCache = new List<ForceComposition>();
        private readonly HashSet<Vector3> _threatened = new HashSet<Vector3>();
        private readonly HashSet<Vector3> _satellitePads = new HashSet<Vector3>();
        private readonly HashSet<Vector3> _mines = new HashSet<Vector3>();
        private readonly List<Vector3> _zoneSites = new List<Vector3>();
        private Vector3 _zoneCentre;
        private Vector3 _zoneHome;
        private bool _relayBeforeMines;
        private readonly Comparison<Vector3> _compareZoneSites;
        // Free sites beside an allied base: income is shared, so they are taken only after other free sites.
        private readonly HashSet<Vector3> _lowPriority = new HashSet<Vector3>();
        private readonly List<Vector3> _contestedTargets = new List<Vector3>();
        private readonly List<Vector3> _openingTargets = new List<Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _assignments = new Dictionary<ISquadron, Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _previous = new Dictionary<ISquadron, Vector3>();
        private readonly List<ISquadron> _picked = new List<ISquadron>();
        private readonly ForceComposition _wingForce = new ForceComposition();

        public bool HasTargets => _targets.Count > 0;

        public EnemySquadronCaptureAssignments() => _compareZoneSites = CompareZoneSites;

        /// <summary>A capture site (not a bait or hold point): a wing near it holds inside the circle.</summary>
        public bool IsSite(Vector3 target) => _targets.ContainsKey(target);

        /// <summary>One wing per opening target, as far as the wings reach.</summary>
        public int GetOpeningWingBudget(int availableWings) => Mathf.Min(_openingTargets.Count, availableWings);

        public void ClearTargets()
        {
            _targets.Clear();
            _defenders.Clear();
            _threatened.Clear();
            _satellitePads.Clear();
            _mines.Clear();
            _lowPriority.Clear();
            _openingTargets.Clear();
        }

        public void AddTarget(Vector3 target, bool threatened = false, bool isSatellitePad = false, bool isMine = false)
        {
            _targets[target] = 0;
            if (threatened) _threatened.Add(target);
            if (isSatellitePad) _satellitePads.Add(target);
            if (isMine) _mines.Add(target);
        }

        /// <summary>The zone holds at least one target, free or contested.</summary>
        public bool HasTargetIn(Vector3 centre, float reach)
        {
            foreach (Vector3 target in _targets.Keys)
                if (PlanarDistanceSquared(target, centre) <= reach * reach) return true;
            return false;
        }

        /// <summary>
        /// Asks <paramref name="fillDefenders"/> for the hostile force at every target added so far. A target left
        /// empty is uncontested and needs one wing.
        /// </summary>
        public void RateTargets(Action<Vector3, ForceComposition> fillDefenders)
        {
            _defenders.Clear();
            foreach (Vector3 target in _targets.Keys)
            {
                if (_defenderCache.Count == _defenders.Count) _defenderCache.Add(new ForceComposition());
                ForceComposition defenders = _defenderCache[_defenders.Count];
                defenders.Clear();
                fillDefenders(target, defenders);
                _defenders.Add(target, defenders);
            }
        }

        /// <summary>Marks unthreatened non-pad targets within <paramref name="radius"/> of an allied base as last choices.</summary>
        public void DeprioritizeNear(Vector3 position, float radius)
        {
            foreach (Vector3 target in _targets.Keys)
                if (!_threatened.Contains(target) && !_satellitePads.Contains(target) &&
                    (target - position).sqrMagnitude <= radius * radius)
                    _lowPriority.Add(target);
        }

        public void PrioritizeArea(Vector3 centre, float radius)
        {
            Vector3 bestPoint = default;
            float bestDistance = float.PositiveInfinity;
            bool bestIsPad = false;
            foreach (Vector3 target in _targets.Keys)
            {
                float distance = (target - centre).sqrMagnitude;
                if (distance > radius * radius || _openingTargets.Contains(target)) continue;
                if (distance < 0.01f)
                {
                    _openingTargets.Add(target);
                    return;
                }
                bool isPad = _satellitePads.Contains(target);
                if (bestIsPad && !isPad || bestIsPad == isPad && distance >= bestDistance) continue;
                bestPoint = target;
                bestDistance = distance;
                bestIsPad = isPad;
            }
            if (!float.IsPositiveInfinity(bestDistance)) _openingTargets.Add(bestPoint);
        }

        /// <param name="addWing">Adds a wing at its current strength to a force; needed once a target is contested.</param>
        public void Assign(IReadOnlyList<ISquadron> squadrons, Action<ISquadron, ForceComposition> addWing = null)
        {
            BeginAssignment();
            AssignRemaining(squadrons, addWing);
        }

        /// <summary>Starts a new pass: the last assignments are remembered so wings keep their target.</summary>
        public void BeginAssignment()
        {
            _previous.Clear();
            foreach (KeyValuePair<ISquadron, Vector3> pair in _assignments)
                _previous.Add(pair.Key, pair.Value);
            _assignments.Clear();
        }

        /// <summary>
        /// Assigns one wing group inside its zone. A wing keeps its free site; otherwise it takes the free site with
        /// the fewest wings, satellite pads first, then relay and mine in the group's order, base-side first. A
        /// contested site is skipped for the next free one; with no free site the wing baits at the zone edge
        /// (<paramref name="bait"/>) and comes back once a site is free. A zone with nothing left is held at its
        /// centre. Wings never leave the zone for a contested target.
        /// </summary>
        public void AssignInZone(IReadOnlyList<ISquadron> wings, Vector3 centre, float reach, Vector3 home,
            bool relayBeforeMines, Vector3 bait)
        {
            _zoneSites.Clear();
            bool hasContested = false;
            foreach (Vector3 target in _targets.Keys)
            {
                if (PlanarDistanceSquared(target, centre) > reach * reach) continue;
                if (IsContested(target)) hasContested = true;
                else _zoneSites.Add(target);
            }
            _zoneCentre = centre;
            _zoneHome = home;
            _relayBeforeMines = relayBeforeMines;
            _zoneSites.Sort(_compareZoneSites);

            foreach (ISquadron wing in wings)
            {
                if (_assignments.ContainsKey(wing)) continue;
                if (_previous.TryGetValue(wing, out Vector3 previous) && _zoneSites.Contains(previous))
                {
                    _assignments.Add(wing, previous);
                    _targets[previous]++;
                    continue;
                }
                bool found = false;
                Vector3 best = default;
                int fewest = int.MaxValue;
                foreach (Vector3 site in _zoneSites)
                {
                    if (_targets[site] >= fewest) continue;
                    best = site;
                    fewest = _targets[site];
                    found = true;
                }
                if (found)
                {
                    _assignments.Add(wing, best);
                    _targets[best]++;
                }
                else _assignments.Add(wing, hasContested ? bait : centre);
            }
        }

        // Pads first, then relay and mine in the group's order; base-side (nearer home) first within a kind.
        private int CompareZoneSites(Vector3 left, Vector3 right)
        {
            int kind = SiteRank(left).CompareTo(SiteRank(right));
            return kind != 0 ? kind :
                PlanarDistanceSquared(left, _zoneHome).CompareTo(PlanarDistanceSquared(right, _zoneHome));
        }

        private int SiteRank(Vector3 site)
        {
            if (_satellitePads.Contains(site)) return 0;
            if (_mines.Contains(site)) return _relayBeforeMines ? 2 : 1;
            bool relay = PlanarDistanceSquared(site, _zoneCentre) < 1f;
            if (relay) return _relayBeforeMines ? 1 : 2;
            return 3;
        }

        private static float PlanarDistanceSquared(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        /// <summary>Spreads every wing not yet assigned in this pass, as <see cref="Assign"/> does.</summary>
        public void AssignRemaining(IReadOnlyList<ISquadron> squadrons, Action<ISquadron, ForceComposition> addWing = null)
        {

            foreach (Vector3 target in _threatened)
                AssignForce(squadrons, target, addWing);
            // Opening: one wing per free zone before any zone gets a second, then contested zones a winning force.
            foreach (Vector3 target in _openingTargets)
                if (!IsContested(target)) AssignForce(squadrons, target, addWing);
            foreach (Vector3 target in _openingTargets)
                AssignForce(squadrons, target, addWing);

            // Fill uncovered sites before adding extra wings to any of them.
            foreach (ISquadron squadron in squadrons)
            {
                if (!_assignments.ContainsKey(squadron) &&
                    _previous.TryGetValue(squadron, out Vector3 target) && _targets.ContainsKey(target))
                    AssignForce(squadrons, target, addWing);
            }

            _contestedTargets.Clear();
            foreach (Vector3 target in _targets.Keys)
                if (IsContested(target)) _contestedTargets.Add(target);
            foreach (Vector3 target in _contestedTargets)
                AssignForce(squadrons, target, addWing);

            foreach (ISquadron squadron in squadrons)
            {
                if (_assignments.ContainsKey(squadron)) continue;
                bool found = false;
                Vector3 best = default;
                int fewest = int.MaxValue;
                float closest = float.MaxValue;
                foreach (KeyValuePair<Vector3, int> candidate in _targets)
                {
                    // A contested target without a winning force stays empty: a lone extra wing would only die there.
                    if (candidate.Value == 0 && IsContested(candidate.Key)) continue;
                    float distance = (candidate.Key - squadron.WorldPosition).sqrMagnitude;
                    bool threatened = _threatened.Contains(candidate.Key);
                    bool bestThreatened = _threatened.Contains(best);
                    bool low = _lowPriority.Contains(candidate.Key);
                    bool bestLow = found && _lowPriority.Contains(best);
                    if (!found || candidate.Value < fewest || candidate.Value == fewest &&
                        (bestLow && !low || bestLow == low &&
                         (threatened && !bestThreatened || threatened == bestThreatened && distance < closest)))
                    {
                        found = true;
                        best = candidate.Key;
                        fewest = candidate.Value;
                        closest = distance;
                    }
                }
                if (!found) continue;
                _assignments.Add(squadron, best);
                _targets[best]++;
            }
        }

        private bool IsContested(Vector3 target) =>
            _defenders.TryGetValue(target, out ForceComposition defenders) && !defenders.IsEmpty;

        /// <summary>
        /// Gives an uncovered target its force: one wing when uncontested, otherwise the closest free wings until
        /// they match the defenders. Assigns nothing when the free wings together are not enough.
        /// </summary>
        private void AssignForce(IReadOnlyList<ISquadron> squadrons, Vector3 target,
            Action<ISquadron, ForceComposition> addWing)
        {
            if (_targets[target] != 0) return;
            bool contested = IsContested(target);
            _picked.Clear();
            _wingForce.Clear();
            while (true)
            {
                ISquadron closest = FindClosestFree(squadrons, target);
                if (closest == null) return;
                _picked.Add(closest);
                if (!contested) break;
                addWing(closest, _wingForce);
                if (CombatMatchup.Advantage(_wingForce, _defenders[target]) >= EVEN_FIGHT) break;
            }
            foreach (ISquadron squadron in _picked)
            {
                _assignments.Add(squadron, target);
                _targets[target]++;
            }
        }

        private ISquadron FindClosestFree(IReadOnlyList<ISquadron> squadrons, Vector3 target)
        {
            ISquadron closest = null;
            float closestDistance = float.MaxValue;
            foreach (ISquadron squadron in squadrons)
            {
                if (_assignments.ContainsKey(squadron) || _picked.Contains(squadron)) continue;
                float distance = (target - squadron.WorldPosition).sqrMagnitude;
                if (_previous.TryGetValue(squadron, out Vector3 previous) && previous == target)
                    distance = -1f;
                if (distance >= closestDistance) continue;
                closest = squadron;
                closestDistance = distance;
            }
            return closest;
        }

        /// <summary>Every target as position, assigned wings and its contested / opening / threatened / pad flags, for telemetry.</summary>
        public string Describe()
        {
            var text = new System.Text.StringBuilder();
            foreach (KeyValuePair<Vector3, int> target in _targets)
                text.Append(FormattableString.Invariant(
                    $"({target.Key.x:F0} {target.Key.z:F0}):got{target.Value}{(IsContested(target.Key) ? $":defenders{_defenders[target.Key].UnitCount}" : "")}{(_openingTargets.Contains(target.Key) ? ":opening" : "")}{(_threatened.Contains(target.Key) ? ":threatened" : "")}{(_satellitePads.Contains(target.Key) ? ":pad" : "")} "));
            return text.ToString();
        }

        public bool TryGetTarget(ISquadron squadron, out Vector3 target) =>
            _assignments.TryGetValue(squadron, out target);

        public void Release(ISquadron squadron)
        {
            _assignments.Remove(squadron);
            _previous.Remove(squadron);
        }
    }
}
