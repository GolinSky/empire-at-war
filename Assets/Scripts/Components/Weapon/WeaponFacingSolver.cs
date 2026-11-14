using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    // Samples actual firing solutions, including how each mount moves around the hull pivot.
    internal sealed class WeaponFacingSolver
    {
        private const int TURN_STEP = 2;
        private const int HEADING_COUNT = 360 / TURN_STEP;
        private const int BURST_PAIR_THRESHOLD = 64;
        private static readonly Vector2[] _headingRotations = BuildHeadingRotations();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker<int, int> _mainMarker =
            new ProfilerMarker<int, int>("Battle.Ship.FacingSolve.Main", "Mounts", "Targets");
        private static readonly ProfilerMarker<int, int> _secondaryMarker =
            new ProfilerMarker<int, int>("Battle.Ship.FacingSolve.Secondary", "Mounts", "Targets");
        private static readonly ProfilerMarker<int, int, int> _sweepMarker =
            new ProfilerMarker<int, int, int>("Battle.Ship.FacingSolve.Sweep", "Headings", "Aim checks", "Early exit");
        private int _aimChecks;
#endif
        private readonly List<Mount> _mainBatteries = new List<Mount>();
        private readonly List<Mount> _secondaryBatteries = new List<Mount>();
        private readonly List<Vector3> _targets = new List<Vector3>();
        private readonly List<float> _targetRadii = new List<float>();
        private readonly List<int> _reachableTargets = new List<int>();
        private Vector2[] _turnedTargets = Array.Empty<Vector2>();
        private int _preparedHeadings;

        public void Clear()
        {
            _mainBatteries.Clear();
            _secondaryBatteries.Clear();
            _targets.Clear();
            _targetRadii.Clear();
            _reachableTargets.Clear();
        }

        public void AddTarget(Vector3 offset)
        {
            _targets.Add(offset);
            _targetRadii.Add(new Vector2(offset.x, offset.z).magnitude);
        }

        public void AddMount(Vector3 offset, Quaternion parentRotation, float range,
            float minYaw, float maxYaw, int weight, bool mainBattery)
        {
            float radius = new Vector2(offset.x, offset.z).magnitude;
            int start = _reachableTargets.Count;
            for (int i = 0; i < _targets.Count; i++)
                if (Mathf.Abs(_targetRadii[i] - radius) <= range)
                    _reachableTargets.Add(i);

            if (start == _reachableTargets.Count) return;
            List<Mount> mounts = mainBattery ? _mainBatteries : _secondaryBatteries;
            mounts.Add(new Mount(offset, parentRotation, range, minYaw, maxYaw, weight, start,
                _reachableTargets.Count));
        }

        public float FindTurn() => FindTurn(out _);

        internal float FindTurn(out int score)
        {
            _preparedHeadings = 0;
            if (_mainBatteries.Count == 0 && _secondaryBatteries.Count == 0)
            {
                score = 0;
                return 0f;
            }
            int required = HEADING_COUNT * _targets.Count;
            if (_turnedTargets.Length < required)
                _turnedTargets = new Vector2[required];

            float turn;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (_mainMarker.Auto(_mainBatteries.Count, _targets.Count))
#endif
                turn = FindTurn(_mainBatteries, out score);
            if (score > 0) return turn;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (_secondaryMarker.Auto(_secondaryBatteries.Count, _targets.Count))
#endif
                return FindTurn(_secondaryBatteries, out score);
        }

        private float FindTurn(List<Mount> mounts, out int bestScore)
        {
            bestScore = 0;
            if (mounts.Count == 0) return 0f;

            int maximumScore = 0;
            for (int i = 0; i < mounts.Count; i++) maximumScore += mounts[i].Weight;

            float bestTurn = 0f;
            if (_reachableTargets.Count >= BURST_PAIR_THRESHOLD)
                return FindTurnBurst(mounts, maximumScore, out bestScore);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _aimChecks = 0;
#endif
            // The closest sample wins ties, including the current heading when it already gives maximum coverage.
            int step = 0;
            for (; step < HEADING_COUNT; step++)
            {
                int score = Score(mounts, step);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTurn = GetTurn(step);
                }

                if (score == maximumScore) break;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (_sweepMarker.Auto(Mathf.Min(step + 1, HEADING_COUNT), _aimChecks,
                       step < HEADING_COUNT - 1 ? 1 : 0)) { }
#endif
            return bestTurn;
        }

        private float FindTurnBurst(List<Mount> mounts, int maximumScore, out int bestScore)
        {
            using var targets = new NativeArray<Vector2>(_targets.Count, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            using var heights = new NativeArray<float>(_targets.Count, Allocator.TempJob);
            using var rotations = new NativeArray<Vector2>(_headingRotations, Allocator.TempJob);
            using var indices = new NativeArray<int>(_reachableTargets.Count, Allocator.TempJob);
            using var input = new NativeArray<WeaponFacingSweepJob.Mount>(mounts.Count, Allocator.TempJob);
            using var bounds = new NativeArray<WeaponFacingSweepJob.Bounds>(HEADING_COUNT, Allocator.TempJob);
            using var offsets = new NativeArray<Vector2>(_targets.Count, Allocator.TempJob);
            var job = new WeaponFacingSweepJob
            {
                Offsets = offsets, Rotations = rotations, Heights = heights, Targets = targets,
                Indices = indices, Mounts = input, Scores = bounds, MaximumScore = maximumScore
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 target = _targets[i];
                job.Offsets[i] = new Vector2(target.x, target.z);
                job.Heights[i] = target.y;
            }
            for (int i = 0; i < indices.Length; i++) job.Indices[i] = _reachableTargets[i];
            for (int i = 0; i < input.Length; i++) job.Mounts[i] = mounts[i].ToJobMount();
            job.Run();

            bestScore = 0;
            int bestTurn = 0;
            int step = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _aimChecks = 0;
#endif
            for (; step < HEADING_COUNT; step++)
            {
                var bound = bounds[step];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _aimChecks += bound.AimChecks;
#endif
                if (bound.Upper > bestScore)
                {
                    // An uncertain pair can change the answer only if this heading can beat the current best.
                    int score = bound.Upper == bound.Lower ? bound.Upper : Score(mounts, step, reuseRotation: false);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTurn = GetTurn(step);
                    }
                }
                if (bestScore == maximumScore) break;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (_sweepMarker.Auto(Mathf.Min(step + 1, HEADING_COUNT), _aimChecks,
                       bestScore == maximumScore && step < HEADING_COUNT - 1 ? 1 : 0)) { }
#endif
            return bestTurn;
        }

        private int Score(List<Mount> mounts, int step, bool reuseRotation = true)
        {
            // Counter-rotate the targets once per sample to keep the mounts in their current frame.
            int start = step * _targets.Count;
            if (!reuseRotation || step >= _preparedHeadings)
            {
                Vector2 rotation = _headingRotations[step];
                for (int i = 0; i < _targets.Count; i++)
                {
                    Vector3 target = _targets[i];
                    _turnedTargets[start + i] = new Vector2(
                        rotation.x * target.x + rotation.y * target.z,
                        -rotation.y * target.x + rotation.x * target.z);
                }
                if (reuseRotation) _preparedHeadings++;
            }

            int score = 0;
            for (int i = 0; i < mounts.Count; i++)
            {
                Mount mount = mounts[i];
                for (int j = mount.TargetStart; j < mount.TargetEnd; j++)
                {
                    int targetIndex = _reachableTargets[j];
                    Vector2 turned = _turnedTargets[start + targetIndex];
                    float dx = turned.x - mount.Offset.x;
                    float dz = turned.y - mount.Offset.z;
                    float distanceSquared = dx * dx + dz * dz;
                    if (distanceSquared > mount.RangeSquared) continue;
                    if (!mount.FullCircle)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        _aimChecks++;
#endif
                        Vector3 target = new Vector3(turned.x, _targets[targetIndex].y, turned.y);
                        if (!mount.IsInArc(target, dx, dz, distanceSquared)) continue;
                    }
                    score += mount.Weight;
                    break;
                }
            }

            return score;
        }

        private static int GetTurn(int step) =>
            (step + 1) / 2 * TURN_STEP * (step % 2 == 0 ? -1 : 1);

        private static Vector2[] BuildHeadingRotations()
        {
            var rotations = new Vector2[HEADING_COUNT];
            for (int i = 0; i < rotations.Length; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(-GetTurn(i), Vector3.up);
                // Use Unity's quaternion coefficients to preserve rounding at range boundaries.
                float doubleY = rotation.y * 2f;
                rotations[i] = new Vector2(1f - rotation.y * doubleY, rotation.w * doubleY);
            }
            return rotations;
        }

        private readonly struct Mount
        {
            public readonly Vector3 Offset;
            public readonly Quaternion ParentRotation;
            public readonly float Range;
            public readonly float RangeSquared;
            private readonly float _innerRangeSquared;
            public readonly float MinYaw;
            public readonly float MaxYaw;
            public readonly bool FullCircle;
            public readonly int Weight;
            public readonly int TargetStart;
            public readonly int TargetEnd;
            private readonly Vector3 _parentRight;
            private readonly Vector3 _parentForward;
            private readonly Vector2 _minEdge;
            private readonly Vector2 _maxEdge;
            private readonly bool _wideArc;
            private readonly bool _validArc;

            public Mount(Vector3 offset, Quaternion parentRotation, float range, float minYaw, float maxYaw,
                int weight, int targetStart, int targetEnd)
            {
                Offset = offset;
                ParentRotation = parentRotation;
                Range = range;
                RangeSquared = range * range;
                _innerRangeSquared = RangeSquared * (1f - 1e-6f);
                MinYaw = minYaw;
                MaxYaw = maxYaw;
                FullCircle = minYaw <= -180f && maxYaw >= 180f;
                Weight = weight;
                TargetStart = targetStart;
                TargetEnd = targetEnd;
                _parentRight = FullCircle ? default : parentRotation * Vector3.right;
                _parentForward = FullCircle ? default : parentRotation * Vector3.forward;
                _minEdge = Edge(minYaw);
                _maxEdge = Edge(maxYaw);
                _wideArc = maxYaw - minYaw > 180f;
                _validArc = minYaw >= -180f && maxYaw <= 180f && minYaw < maxYaw;
            }

            private static Vector2 Edge(float yaw)
            {
                float radians = yaw * Mathf.Deg2Rad;
                return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            }

            public bool IsInArc(Vector3 target, float dx, float dz, float distanceSquared)
            {
                int result = ToJobMount().Classify(dx, target.y - Offset.y, dz, distanceSquared);
                if (result != 0) return result > 0;
                return WeaponTargetSelector.TryCalculateAim(target, Offset, ParentRotation, Range,
                    MinYaw, MaxYaw, out _, out _);
            }

            public WeaponFacingSweepJob.Mount ToJobMount() => new WeaponFacingSweepJob.Mount
            {
                Offset = Offset, Right = _parentRight, Forward = _parentForward, MinEdge = _minEdge,
                MaxEdge = _maxEdge, RangeSquared = RangeSquared, InnerRangeSquared = _innerRangeSquared,
                Weight = Weight, Start = TargetStart, End = TargetEnd, FullCircle = FullCircle,
                WideArc = _wideArc, ValidArc = _validArc
            };
        }
    }
}
