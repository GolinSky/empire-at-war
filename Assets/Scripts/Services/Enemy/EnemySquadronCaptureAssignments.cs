using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemySquadronCaptureAssignments
    {
        private readonly Dictionary<Vector3, int> _targets = new Dictionary<Vector3, int>();
        private readonly Dictionary<Vector3, int> _requiredWings = new Dictionary<Vector3, int>();
        private readonly HashSet<Vector3> _threatened = new HashSet<Vector3>();
        private readonly HashSet<Vector3> _satellitePads = new HashSet<Vector3>();
        // Free sites beside an allied base: income is shared, so they are taken only after other free sites.
        private readonly HashSet<Vector3> _lowPriority = new HashSet<Vector3>();
        private readonly List<Vector3> _contestedTargets = new List<Vector3>();
        private readonly List<Vector3> _openingTargets = new List<Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _assignments = new Dictionary<ISquadron, Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _previous = new Dictionary<ISquadron, Vector3>();

        public bool HasTargets => _targets.Count > 0;

        public int GetOpeningWingBudget(int availableWings)
        {
            int required = 0;
            foreach (Vector3 target in _openingTargets)
            {
                int needed = _requiredWings[target];
                if (required + needed <= availableWings) required += needed;
            }

            return required;
        }

        public void ClearTargets()
        {
            _targets.Clear();
            _requiredWings.Clear();
            _threatened.Clear();
            _satellitePads.Clear();
            _lowPriority.Clear();
            _openingTargets.Clear();
        }

        public void AddTarget(Vector3 target, bool threatened = false, bool isSatellitePad = false,
            int requiredWings = 1)
        {
            _targets[target] = 0;
            _requiredWings[target] = requiredWings;
            if (threatened) _threatened.Add(target);
            if (isSatellitePad) _satellitePads.Add(target);
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

        public void Assign(IReadOnlyList<ISquadron> squadrons)
        {
            _previous.Clear();
            foreach (KeyValuePair<ISquadron, Vector3> pair in _assignments)
                _previous.Add(pair.Key, pair.Value);
            _assignments.Clear();

            foreach (Vector3 target in _threatened)
                AssignGroup(squadrons, target);
            foreach (Vector3 target in _openingTargets)
                AssignGroup(squadrons, target);

            // Fill uncovered sites before adding extra wings to any of them.
            foreach (ISquadron squadron in squadrons)
            {
                if (!_assignments.ContainsKey(squadron) &&
                    _previous.TryGetValue(squadron, out Vector3 target) && _targets.ContainsKey(target))
                    AssignGroup(squadrons, target);
            }

            _contestedTargets.Clear();
            foreach (Vector3 target in _targets.Keys)
                if (_requiredWings[target] > 1) _contestedTargets.Add(target);
            foreach (Vector3 target in _contestedTargets)
                AssignGroup(squadrons, target);

            foreach (ISquadron squadron in squadrons)
            {
                if (_assignments.ContainsKey(squadron)) continue;
                bool found = false;
                Vector3 best = default;
                int fewest = int.MaxValue;
                float closest = float.MaxValue;
                foreach (KeyValuePair<Vector3, int> candidate in _targets)
                {
                    if (candidate.Value == 0 && _requiredWings[candidate.Key] > 1) continue;
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

        private void AssignGroup(IReadOnlyList<ISquadron> squadrons, Vector3 target)
        {
            int required = _requiredWings[target];
            if (_targets[target] != 0 || squadrons.Count - _assignments.Count < required) return;
            for (int i = 0; i < required; i++) AssignClosest(squadrons, target);
        }

        private void AssignClosest(IReadOnlyList<ISquadron> squadrons, Vector3 target)
        {
            ISquadron closest = null;
            float closestDistance = float.MaxValue;
            foreach (ISquadron squadron in squadrons)
            {
                float distance = (target - squadron.WorldPosition).sqrMagnitude;
                if (_assignments.ContainsKey(squadron)) continue;
                if (_previous.TryGetValue(squadron, out Vector3 previous) && previous == target)
                    distance = -1f;
                if (distance >= closestDistance) continue;
                closest = squadron;
                closestDistance = distance;
            }
            _assignments.Add(closest, target);
            _targets[target]++;
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
