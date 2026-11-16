using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Shared team visit history. Sampling and world queries belong to EnemyPatrolService.</summary>
    public sealed class EnemyPatrolMemory
    {
        private readonly Dictionary<string, EnemyPatrolPoint> _points = new Dictionary<string, EnemyPatrolPoint>();
        private static readonly Comparison<RankedPoint> RANK_POINTS = CompareRankedPoints;
        private readonly List<RankedPoint> _ranked = new List<RankedPoint>();
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

        /// <summary>
        /// The highest-scoring eligible point; ties keep enumeration order. Eligibility (path and threat queries)
        /// is evaluated in score order and stops after the best and runner-up eligible points.
        /// </summary>
        public EnemyPatrolPoint Select(FormationPoint origin, float now, Func<EnemyPatrolPoint, bool> eligible,
            out float runnerUp)
        {
            _ranked.Clear();
            int index = 0;
            foreach (EnemyPatrolPoint point in _points.Values)
            {
                float score = point.Score(origin, now);
                if (score > 0f) _ranked.Add(new RankedPoint(point, score, index));
                index++;
            }
            _ranked.Sort(RANK_POINTS);

            EnemyPatrolPoint best = null;
            runnerUp = 0f;
            foreach (RankedPoint ranked in _ranked)
            {
                if (!eligible(ranked.Point)) continue;
                if (best == null)
                {
                    best = ranked.Point;
                    continue;
                }
                runnerUp = ranked.Score;
                break;
            }
            return best;
        }

        private static int CompareRankedPoints(RankedPoint left, RankedPoint right)
        {
            int score = right.Score.CompareTo(left.Score);
            return score != 0 ? score : left.Index.CompareTo(right.Index);
        }

        private readonly struct RankedPoint
        {
            public readonly EnemyPatrolPoint Point;
            public readonly float Score;
            public readonly int Index;

            public RankedPoint(EnemyPatrolPoint point, float score, int index)
            {
                Point = point;
                Score = score;
                Index = index;
            }
        }
    }
}
