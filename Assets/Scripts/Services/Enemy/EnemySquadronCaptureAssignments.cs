using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemySquadronCaptureAssignments
    {
        private readonly Dictionary<Vector3, int> _targets = new Dictionary<Vector3, int>();
        private readonly HashSet<Vector3> _threatened = new HashSet<Vector3>();
        private readonly HashSet<Vector3> _satellitePads = new HashSet<Vector3>();
        private readonly List<Vector3> _openingTargets = new List<Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _assignments = new Dictionary<ISquadron, Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _previous = new Dictionary<ISquadron, Vector3>();

        public bool HasTargets => _targets.Count > 0;

        public void ClearTargets()
        {
            _targets.Clear();
            _threatened.Clear();
            _satellitePads.Clear();
            _openingTargets.Clear();
        }

        public void AddTarget(Vector3 target, bool threatened = false, bool isSatellitePad = false)
        {
            _targets[target] = 0;
            if (threatened) _threatened.Add(target);
            if (isSatellitePad) _satellitePads.Add(target);
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
                if (!AssignClosest(squadrons, target)) break;
            foreach (Vector3 target in _openingTargets)
            {
                if (_targets[target] > 0) continue;
                ISquadron previous = null;
                foreach (ISquadron squadron in squadrons)
                {
                    if (_assignments.ContainsKey(squadron) || !_previous.TryGetValue(squadron, out Vector3 point) ||
                        point != target) continue;
                    previous = squadron;
                    break;
                }
                if (previous != null)
                {
                    _assignments.Add(previous, target);
                    _targets[target] = 1;
                }
                else if (!AssignClosest(squadrons, target)) break;
            }

            // Keep one squadron on each still-valid objective before filling uncovered objectives.
            foreach (ISquadron squadron in squadrons)
            {
                if (_assignments.ContainsKey(squadron) || !_previous.TryGetValue(squadron, out Vector3 target) ||
                    !_targets.TryGetValue(target, out int count) || count > 0) continue;
                _assignments.Add(squadron, target);
                _targets[target] = 1;
            }

            foreach (ISquadron squadron in squadrons)
            {
                if (_assignments.ContainsKey(squadron)) continue;
                bool found = false;
                Vector3 best = default;
                int fewest = int.MaxValue;
                float closest = float.MaxValue;
                foreach (KeyValuePair<Vector3, int> candidate in _targets)
                {
                    float distance = (candidate.Key - squadron.WorldPosition).sqrMagnitude;
                    bool threatened = _threatened.Contains(candidate.Key);
                    bool bestThreatened = _threatened.Contains(best);
                    if (!found || candidate.Value < fewest || candidate.Value == fewest &&
                        (threatened && !bestThreatened || threatened == bestThreatened && distance < closest))
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

        private bool AssignClosest(IReadOnlyList<ISquadron> squadrons, Vector3 target)
        {
            ISquadron closest = null;
            float closestDistance = float.MaxValue;
            foreach (ISquadron squadron in squadrons)
            {
                float distance = (target - squadron.WorldPosition).sqrMagnitude;
                if (_assignments.ContainsKey(squadron) || distance >= closestDistance) continue;
                closest = squadron;
                closestDistance = distance;
            }
            if (closest == null) return false;
            _assignments.Add(closest, target);
            _targets[target] = 1;
            return true;
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
