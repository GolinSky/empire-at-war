using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Shared team visit history. Sampling and world queries belong to EnemyPatrolService.</summary>
    public sealed class EnemyPatrolMemory
    {
        private readonly Dictionary<string, EnemyPatrolPoint> _points = new Dictionary<string, EnemyPatrolPoint>();
        public IReadOnlyCollection<EnemyPatrolPoint> Entries => _points.Values;
        public float NextSampleTime { get; set; }
        public float NextCoverageTime { get; set; }

        public EnemyPatrolPoint Register(string id, FormationPoint position, string kind, float value)
        {
            if (!_points.TryGetValue(id, out EnemyPatrolPoint point))
            {
                point = new EnemyPatrolPoint(id, position, kind, value);
                _points.Add(id, point);
            }
            point.Position = position;
            point.Kind = kind;
            point.Value = value;
            return point;
        }

        public bool TryGet(string id, out EnemyPatrolPoint point) => _points.TryGetValue(id, out point);

        public void Remove(string id) => _points.Remove(id);

        public void Observe(string id, float now)
        {
            EnemyPatrolPoint point = _points[id];
            point.LastVisitStaleness = point.Staleness(now);
            point.LastSeenTime = now;
        }

        public EnemyPatrolPoint Select(FormationPoint origin, float now, Func<EnemyPatrolPoint, bool> eligible,
            out float runnerUp)
        {
            EnemyPatrolPoint best = null;
            float bestScore = 0f;
            runnerUp = 0f;
            foreach (EnemyPatrolPoint point in _points.Values)
            {
                float score = point.Score(origin, now);
                if (score <= 0f || !eligible(point)) continue;
                if (score > bestScore)
                {
                    runnerUp = bestScore;
                    bestScore = score;
                    best = point;
                }
                else runnerUp = Math.Max(runnerUp, score);
            }
            return best;
        }
    }
}
