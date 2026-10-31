using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemySquadronCaptureAssignments
    {
        private readonly Dictionary<Vector3, int> _targets = new Dictionary<Vector3, int>();
        private readonly HashSet<Vector3> _threatened = new HashSet<Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _assignments = new Dictionary<ISquadron, Vector3>();
        private readonly Dictionary<ISquadron, Vector3> _previous = new Dictionary<ISquadron, Vector3>();

        public void ClearTargets()
        {
            _targets.Clear();
            _threatened.Clear();
        }

        public void AddTarget(Vector3 target, bool threatened = false)
        {
            _targets[target] = 0;
            if (threatened) _threatened.Add(target);
        }

        public void Assign(IReadOnlyList<ISquadron> squadrons)
        {
            _previous.Clear();
            foreach (KeyValuePair<ISquadron, Vector3> pair in _assignments)
                _previous.Add(pair.Key, pair.Value);
            _assignments.Clear();

            foreach (Vector3 target in _threatened)
            {
                ISquadron closestSquadron = null;
                float closestDistance = float.MaxValue;
                foreach (ISquadron squadron in squadrons)
                {
                    float distance = (target - squadron.WorldPosition).sqrMagnitude;
                    if (_assignments.ContainsKey(squadron) || distance >= closestDistance) continue;
                    closestSquadron = squadron;
                    closestDistance = distance;
                }
                if (closestSquadron == null) break;
                _assignments.Add(closestSquadron, target);
                _targets[target] = 1;
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

        public bool TryGetTarget(ISquadron squadron, out Vector3 target) =>
            _assignments.TryGetValue(squadron, out target);

        public void Release(ISquadron squadron)
        {
            _assignments.Remove(squadron);
            _previous.Remove(squadron);
        }
    }
}
