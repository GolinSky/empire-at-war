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
    /// Wing groups choose inside their own zone first (<see cref="AssignInZone"/>): they land on mining spots, where
    /// the mine and the satellite pads in reach flip together. The rest are spread after.
    /// </summary>
    public sealed class EnemySquadronCaptureAssignments
    {
        /// <summary>Wing power over the defenders needed to go in: an even fight.</summary>
        private const float EVEN_FIGHT = 1f;
        /// <summary>A group this large splits in half over the two best mining spots of its zone.</summary>
        public const int SPLIT_GROUP_WINGS = 6;

        private readonly Dictionary<Vector3, int> _targets = new Dictionary<Vector3, int>();
        private readonly Dictionary<Vector3, ForceComposition> _defenders = new Dictionary<Vector3, ForceComposition>();
        private readonly List<ForceComposition> _defenderCache = new List<ForceComposition>();
        private readonly HashSet<Vector3> _threatened = new HashSet<Vector3>();
        private readonly HashSet<Vector3> _satellitePads = new HashSet<Vector3>();
        private readonly HashSet<Vector3> _mines = new HashSet<Vector3>();
        // Capture radius per satellite pad: a wing standing within it of the pad captures the pad.
        private readonly Dictionary<Vector3, float> _padRadius = new Dictionary<Vector3, float>();
        // Every mining spot with its capture radius, whoever owns the mine: pads around it are taken from there.
        private readonly Dictionary<Vector3, float> _spots = new Dictionary<Vector3, float>();
        private readonly List<Vector3> _zoneSpots = new List<Vector3>();
        private readonly List<Vector3> _zoneSites = new List<Vector3>();
        private readonly List<ISquadron> _part = new List<ISquadron>();
        // Wings sent to an uncontested mining spot: they hold inside its circle (a move, not an attack-move).
        private readonly Dictionary<ISquadron, float> _holds = new Dictionary<ISquadron, float>();
        private Vector3 _zoneHome;
        private readonly Comparison<Vector3> _compareByHome;
        // Free sites beside an allied base: income is shared, so they are taken only after other free sites.
        private readonly HashSet<Vector3> _lowPriority = new HashSet<Vector3>();
        private readonly List<Vector3> _contestedTargets = new List<Vector3>();
        private readonly List<Vector3> _openingTargets = new List<Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _assignments = new Dictionary<ISquadron, Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _previous = new Dictionary<ISquadron, Vector3>();
        private readonly List<ISquadron> _picked = new List<ISquadron>();
        private readonly ForceComposition _wingForce = new ForceComposition();

        public bool HasTargets => _targets.Count > 0;

        public EnemySquadronCaptureAssignments() => _compareByHome = CompareByHome;

        /// <summary>
        /// The wing goes to an uncontested mining spot: once inside <paramref name="radius"/> of it, it holds there
        /// with a move order until the mine and the pads around flip.
        /// </summary>
        public bool TryGetHold(ISquadron squadron, out float radius) => _holds.TryGetValue(squadron, out radius);

        /// <summary>One wing per opening target, as far as the wings reach.</summary>
        public int GetOpeningWingBudget(int availableWings) => Mathf.Min(_openingTargets.Count, availableWings);

        public void ClearTargets()
        {
            _targets.Clear();
            _defenders.Clear();
            _threatened.Clear();
            _satellitePads.Clear();
            _mines.Clear();
            _padRadius.Clear();
            _spots.Clear();
            _lowPriority.Clear();
            _openingTargets.Clear();
        }

        /// <param name="captureRadius">Capture circle of a satellite pad; a wing inside it captures the pad.</param>
        public void AddTarget(Vector3 target, bool threatened = false, bool isSatellitePad = false, bool isMine = false,
            float captureRadius = 0f)
        {
            _targets[target] = 0;
            if (threatened) _threatened.Add(target);
            if (isSatellitePad)
            {
                _satellitePads.Add(target);
                _padRadius[target] = captureRadius;
            }
            if (isMine) _mines.Add(target);
        }

        /// <summary>A mining site, captured or not: wings landing inside <paramref name="radius"/> take it and the pads in reach.</summary>
        public void AddMiningSpot(Vector3 spot, float radius) => _spots[spot] = radius;

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
            foreach (Vector3 target in _targets.Keys) Rate(target, fillDefenders);
            foreach (Vector3 spot in _spots.Keys)
                if (!_defenders.ContainsKey(spot)) Rate(spot, fillDefenders);
        }

        private void Rate(Vector3 point, Action<Vector3, ForceComposition> fillDefenders)
        {
            if (_defenderCache.Count == _defenders.Count) _defenderCache.Add(new ForceComposition());
            ForceComposition defenders = _defenderCache[_defenders.Count];
            defenders.Clear();
            fillDefenders(point, defenders);
            _defenders.Add(point, defenders);
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
            _holds.Clear();
        }

        /// <summary>
        /// Assigns one wing group inside its zone. The group lands on a mining spot that still has something to take
        /// (its mine or a satellite pad in reach), base-side first; a group of <see cref="SPLIT_GROUP_WINGS"/> or more
        /// splits in half over the two best spots. An uncontested spot is held from inside its circle; a contested
        /// one is fought for when the wings at least match its defenders, else the next spot is tried. With no spot
        /// left the wings take the remaining site nearest home (free first, else they fight for it) and, with nothing
        /// left at all, hold the zone centre. A group is never left idle.
        /// </summary>
        /// <param name="addWing">Adds a wing at its current strength to a force, to rate a contested spot.</param>
        public void AssignInZone(IReadOnlyList<ISquadron> wings, Vector3 centre, float reach, Vector3 home,
            Action<ISquadron, ForceComposition> addWing)
        {
            if (wings.Count == 0) return;
            _zoneHome = home;
            _zoneSpots.Clear();
            foreach (Vector3 spot in _spots.Keys)
                if (PlanarDistanceSquared(spot, centre) <= reach * reach && HasWorkAt(spot)) _zoneSpots.Add(spot);
            _zoneSpots.Sort(_compareByHome);
            _zoneSites.Clear();
            foreach (Vector3 target in _targets.Keys)
                if (PlanarDistanceSquared(target, centre) <= reach * reach) _zoneSites.Add(target);
            _zoneSites.Sort(_compareByHome);

            bool split = wings.Count >= SPLIT_GROUP_WINGS && _zoneSpots.Count >= 2;
            int firstPart = split ? wings.Count / 2 : wings.Count;
            Vector3? taken = AssignPart(wings, 0, firstPart, centre, null, addWing);
            if (split) AssignPart(wings, firstPart, wings.Count, centre, taken, addWing);
        }

        /// <returns>The mining spot the part took, or null when it went elsewhere.</returns>
        private Vector3? AssignPart(IReadOnlyList<ISquadron> wings, int from, int to, Vector3 centre, Vector3? skip,
            Action<ISquadron, ForceComposition> addWing)
        {
            _part.Clear();
            for (int i = from; i < to; i++)
                if (!_assignments.ContainsKey(wings[i])) _part.Add(wings[i]);
            if (_part.Count == 0) return null;

            foreach (Vector3 spot in _zoneSpots)
            {
                if (skip.HasValue && skip.Value == spot) continue;
                bool contested = IsContested(spot);
                if (contested && !Wins(spot, addWing)) continue;
                AssignPartTo(spot, contested ? (float?)null : _spots[spot]);
                return spot;
            }

            // No mining spot is left or winnable: the remaining site nearest home, free first, else fight for it.
            foreach (Vector3 site in _zoneSites)
            {
                if (IsContested(site)) continue;
                AssignPartTo(site, null);
                return null;
            }
            AssignPartTo(_zoneSites.Count > 0 ? _zoneSites[0] : centre, null);
            return null;
        }

        private void AssignPartTo(Vector3 target, float? holdRadius)
        {
            foreach (ISquadron wing in _part)
            {
                _assignments.Add(wing, target);
                if (_targets.ContainsKey(target)) _targets[target]++;
                if (holdRadius.HasValue) _holds.Add(wing, holdRadius.Value);
            }
        }

        private bool Wins(Vector3 spot, Action<ISquadron, ForceComposition> addWing)
        {
            _wingForce.Clear();
            foreach (ISquadron wing in _part) addWing(wing, _wingForce);
            return CombatMatchup.Advantage(_wingForce, _defenders[spot]) >= EVEN_FIGHT;
        }

        /// <summary>The spot's mine or a satellite pad whose circle covers the spot can still be taken.</summary>
        private bool HasWorkAt(Vector3 spot)
        {
            if (_targets.ContainsKey(spot)) return true;
            foreach (Vector3 pad in _satellitePads)
            {
                float radius = _padRadius[pad];
                if (PlanarDistanceSquared(pad, spot) <= radius * radius) return true;
            }
            return false;
        }

        // Base-side (nearer home) first.
        private int CompareByHome(Vector3 left, Vector3 right) =>
            PlanarDistanceSquared(left, _zoneHome).CompareTo(PlanarDistanceSquared(right, _zoneHome));

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

        /// <summary>
        /// Every target as position, assigned wings and its contested / opening / threatened / pad / mine flags, then
        /// the mining spots wings landed on, for telemetry.
        /// </summary>
        public string Describe()
        {
            var text = new System.Text.StringBuilder();
            foreach (KeyValuePair<Vector3, int> target in _targets)
                text.Append(FormattableString.Invariant(
                    $"({target.Key.x:F0} {target.Key.z:F0}):got{target.Value}{(IsContested(target.Key) ? $":defenders{_defenders[target.Key].UnitCount}" : "")}{(_openingTargets.Contains(target.Key) ? ":opening" : "")}{(_threatened.Contains(target.Key) ? ":threatened" : "")}{(_satellitePads.Contains(target.Key) ? ":pad" : "")}{(_mines.Contains(target.Key) ? ":mine" : "")} "));
            text.Append("spots=");
            foreach (Vector3 spot in _spots.Keys)
            {
                int landed = 0;
                foreach (Vector3 assigned in _assignments.Values)
                    if (assigned == spot) landed++;
                if (landed > 0) text.Append(FormattableString.Invariant($"({spot.x:F0} {spot.z:F0}):wings{landed} "));
            }
            return text.ToString();
        }

        public bool TryGetTarget(ISquadron squadron, out Vector3 target) =>
            _assignments.TryGetValue(squadron, out target);

        public void Release(ISquadron squadron)
        {
            _assignments.Remove(squadron);
            _holds.Remove(squadron);
            _previous.Remove(squadron);
        }
    }
}
