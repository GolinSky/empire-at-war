using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    // Samples actual firing solutions, including how each mount moves around the hull pivot.
    internal sealed class WeaponFacingSolver
    {
        private const int TURN_STEP = 2;
        private readonly List<Mount> _mainBatteries = new List<Mount>();
        private readonly List<Mount> _secondaryBatteries = new List<Mount>();
        private readonly List<Vector3> _targets = new List<Vector3>();
        private readonly List<float> _targetRadii = new List<float>();
        private readonly List<int> _reachableTargets = new List<int>();
        private Vector3[] _turnedTargets = Array.Empty<Vector3>();

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

        public float FindTurn()
        {
            if (_turnedTargets.Length < _targets.Count)
                _turnedTargets = new Vector3[_targets.Count];

            float turn = FindTurn(_mainBatteries, out int score);
            return score > 0 ? turn : FindTurn(_secondaryBatteries, out _);
        }

        private float FindTurn(List<Mount> mounts, out int bestScore)
        {
            bestScore = 0;
            if (mounts.Count == 0) return 0f;

            int maximumScore = 0;
            for (int i = 0; i < mounts.Count; i++) maximumScore += mounts[i].Weight;

            float bestTurn = 0f;
            // The closest sample wins ties, including the current heading when it already gives maximum coverage.
            for (int step = 0; step < 360 / TURN_STEP; step++)
            {
                int turn = (step + 1) / 2 * TURN_STEP * (step % 2 == 0 ? -1 : 1);
                int score = Score(mounts, turn);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTurn = turn;
                }

                if (score == maximumScore) break;
            }

            return bestTurn;
        }

        private int Score(List<Mount> mounts, int turn)
        {
            // Counter-rotate the targets once per sample to keep the mounts in their current frame.
            Quaternion rotation = Quaternion.AngleAxis(-turn, Vector3.up);
            for (int i = 0; i < _targets.Count; i++) _turnedTargets[i] = rotation * _targets[i];

            int score = 0;
            for (int i = 0; i < mounts.Count; i++)
            {
                Mount mount = mounts[i];
                for (int j = mount.TargetStart; j < mount.TargetEnd; j++)
                {
                    Vector3 target = _turnedTargets[_reachableTargets[j]];
                    Vector3 direction = target - mount.Offset;
                    if (direction.x * direction.x + direction.z * direction.z > mount.RangeSquared) continue;
                    if (!mount.FullCircle && !WeaponTargetSelector.TryCalculateAim(target, mount.Offset,
                            mount.ParentRotation, mount.Range, mount.MinYaw, mount.MaxYaw, out _, out _)) continue;
                    score += mount.Weight;
                    break;
                }
            }

            return score;
        }

        private readonly struct Mount
        {
            public readonly Vector3 Offset;
            public readonly Quaternion ParentRotation;
            public readonly float Range;
            public readonly float RangeSquared;
            public readonly float MinYaw;
            public readonly float MaxYaw;
            public readonly bool FullCircle;
            public readonly int Weight;
            public readonly int TargetStart;
            public readonly int TargetEnd;

            public Mount(Vector3 offset, Quaternion parentRotation, float range, float minYaw, float maxYaw,
                int weight, int targetStart, int targetEnd)
            {
                Offset = offset;
                ParentRotation = parentRotation;
                Range = range;
                RangeSquared = range * range;
                MinYaw = minYaw;
                MaxYaw = maxYaw;
                FullCircle = minYaw <= -180f && maxYaw >= 180f;
                Weight = weight;
                TargetStart = targetStart;
                TargetEnd = targetEnd;
            }
        }
    }
}
