using System;
using System.Collections.Generic;
using System.Numerics;

namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    /// <summary>
    /// Shared by one spatial pace cluster of a move, attack, or attack-move order so nearby units approach together.
    /// Members report their own uncapped speed, straight-line remaining distance, position, and heading; a member
    /// that cannot move or is recovering reports <see cref="float.PositiveInfinity"/> so it never holds the cluster back.
    /// </summary>
    public sealed class PaceCluster
    {
        private const int MINIMUM_GROUP = 2;

        private readonly List<float> _speeds = new List<float>();
        private readonly List<float> _remaining = new List<float>();
        private readonly List<Vector2> _positions = new List<Vector2>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<Vector2> _headings = new List<Vector2>();
        private readonly List<int> _forwardedMembers = new List<int>();

        private PaceCluster _mergedInto;
        private float _issuePace;
        private int _memberCount;
        private float _pace = float.PositiveInfinity;
        private float _meanRemaining = float.NaN;
        private Vector2 _centroid;
        private float _radius;
        private Vector2 _heading;
        private bool _paceDirty;
        private bool _remainingDirty;
        private bool _geometryDirty;

        public int OrderId { get; }
        public PaceClusterTuning Tuning { get; }
        public bool IsMerged => _mergedInto != null;

        public int MemberCount => _mergedInto != null ? _mergedInto.MemberCount : _memberCount;

        public float IssuePace => _mergedInto != null ? _mergedInto.IssuePace : _issuePace;

        /// <summary>The target the cluster fights on after its order's target died; the first ship to find one sets it.</summary>

        /// <summary>
        /// Slowest member speed meeting the straggler speed floor, or <see cref="float.PositiveInfinity"/> when fewer
        /// than two members remain.
        /// </summary>
        public float Pace
        {
            get
            {
                if (_mergedInto != null) return _mergedInto.Pace;
                if (_paceDirty) RecomputePace();
                return _pace;
            }
        }

        /// <summary>Average straight-line remaining distance of active members, or NaN if none.</summary>
        public float MeanRemaining
        {
            get
            {
                if (_mergedInto != null) return _mergedInto.MeanRemaining;
                if (_remainingDirty) RecomputeRemaining();
                return _meanRemaining;
            }
        }

        public Vector2 Centroid
        {
            get
            {
                if (_mergedInto != null) return _mergedInto.Centroid;
                if (_geometryDirty) RecomputeGeometry();
                return _centroid;
            }
        }

        public float Radius
        {
            get
            {
                if (_mergedInto != null) return _mergedInto.Radius;
                if (_geometryDirty) RecomputeGeometry();
                return _radius;
            }
        }

        public Vector2 Heading
        {
            get
            {
                if (_mergedInto != null) return _mergedInto.Heading;
                if (_geometryDirty) RecomputeGeometry();
                return _heading;
            }
        }

        public PaceCluster(int orderId = 0, float issuePace = float.PositiveInfinity, PaceClusterTuning? tuning = null)
        {
            OrderId = orderId;
            _issuePace = issuePace;
            Tuning = tuning ?? PaceClusterTuning.Default;
        }

        public bool IsBelowStragglerSpeed(float speed)
        {
            if (_mergedInto != null) return _mergedInto.IsBelowStragglerSpeed(speed);
            return !float.IsInfinity(_issuePace) && !float.IsNaN(_issuePace) &&
                   !float.IsInfinity(speed) && !float.IsNaN(speed) &&
                   speed < _issuePace * Tuning.StragglerSpeedRatio;
        }

        public int Join(float speed, float remaining = float.NaN, Vector2 position = default,
            float radius = 0f, Vector2 heading = default)
        {
            if (_mergedInto != null)
            {
                int forwarded = _mergedInto.Join(speed, remaining, position, radius, heading);
                _speeds.Add(float.NaN);
                _remaining.Add(float.NaN);
                _positions.Add(default);
                _radii.Add(0f);
                _headings.Add(default);
                _forwardedMembers.Add(forwarded);
                return _speeds.Count - 1;
            }

            if (!float.IsInfinity(speed) && !float.IsNaN(speed) && speed < _issuePace)
                _issuePace = speed;

            _speeds.Add(speed);
            _remaining.Add(remaining);
            _positions.Add(position);
            _radii.Add(radius);
            _headings.Add(heading);
            _forwardedMembers.Add(-1);
            _memberCount++;
            _paceDirty = true;
            _remainingDirty = true;
            _geometryDirty = true;
            return _speeds.Count - 1;
        }

        public void Leave(int member)
        {
            if (_mergedInto != null)
            {
                int forwarded = _forwardedMembers[member];
                if (forwarded >= 0)
                {
                    _forwardedMembers[member] = -1;
                    _mergedInto.Leave(forwarded);
                }
                return;
            }

            if (float.IsNaN(_speeds[member])) return;
            _speeds[member] = float.NaN;
            _remaining[member] = float.NaN;
            _memberCount--;
            _paceDirty = true;
            _remainingDirty = true;
            _geometryDirty = true;
        }

        public void Report(int member, float speed, float remaining = float.NaN,
            Vector2 position = default, Vector2 heading = default)
        {
            if (_mergedInto != null)
            {
                int forwarded = _forwardedMembers[member];
                if (forwarded >= 0) _mergedInto.Report(forwarded, speed, remaining, position, heading);
                return;
            }

            if (float.IsNaN(_speeds[member])) return;
            if (_speeds[member] != speed)
            {
                _speeds[member] = speed;
                _paceDirty = true;
            }

            if (!AreEqual(_remaining[member], remaining))
            {
                _remaining[member] = remaining;
                _remainingDirty = true;
            }

            if (_positions[member] != position || _headings[member] != heading)
            {
                _positions[member] = position;
                _headings[member] = heading;
                _geometryDirty = true;
            }
        }

        /// <summary>
        /// Merges this cluster into <paramref name="target"/> without notifying member units. Subsequent calls from
        /// members still holding a reference to this cluster forward transparently to <paramref name="target"/>.
        /// </summary>
        public void MergeInto(PaceCluster target)
        {
            PaceCluster source = Resolve();
            PaceCluster destination = target.Resolve();
            if (ReferenceEquals(source, destination)) return;


            for (int i = 0; i < source._speeds.Count; i++)
            {
                if (float.IsNaN(source._speeds[i]))
                {
                    source._forwardedMembers[i] = -1;
                    continue;
                }

                int newIndex = destination.Join(
                    source._speeds[i],
                    source._remaining[i],
                    source._positions[i],
                    source._radii[i],
                    source._headings[i]);
                source._forwardedMembers[i] = newIndex;
                source._speeds[i] = float.NaN;
                source._remaining[i] = float.NaN;
            }

            source._memberCount = 0;
            source._pace = float.PositiveInfinity;
            source._meanRemaining = float.NaN;
            source._paceDirty = false;
            source._remainingDirty = false;
            source._geometryDirty = false;
            source._mergedInto = destination;
        }

        public PaceCluster Resolve()
        {
            PaceCluster current = this;
            while (current._mergedInto != null) current = current._mergedInto;
            return current;
        }

        public bool TryGetCentroid(out Vector2 centroid)
        {
            if (_mergedInto != null) return _mergedInto.TryGetCentroid(out centroid);
            if (_memberCount == 0)
            {
                centroid = default;
                return false;
            }

            if (_geometryDirty) RecomputeGeometry();
            centroid = _centroid;
            return true;
        }

        private void RecomputePace()
        {
            _paceDirty = false;
            _pace = float.PositiveInfinity;
            if (_memberCount < MINIMUM_GROUP) return;

            float floor = !float.IsInfinity(_issuePace) && !float.IsNaN(_issuePace)
                ? _issuePace * Tuning.StragglerSpeedRatio
                : 0f;

            for (int i = 0; i < _speeds.Count; i++)
            {
                float speed = _speeds[i];
                if (float.IsNaN(speed) || speed < floor) continue;
                if (speed < _pace) _pace = speed;
            }
        }

        private void RecomputeRemaining()
        {
            _remainingDirty = false;
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < _speeds.Count; i++)
            {
                if (float.IsNaN(_speeds[i])) continue;
                float rem = _remaining[i];
                if (float.IsNaN(rem) || float.IsInfinity(rem)) continue;
                sum += rem;
                count++;
            }

            _meanRemaining = count > 0 ? sum / count : float.NaN;
        }

        private void RecomputeGeometry()
        {
            _geometryDirty = false;
            if (_memberCount == 0)
            {
                _centroid = default;
                _radius = 0f;
                _heading = default;
                return;
            }

            Vector2 positionSum = Vector2.Zero;
            Vector2 headingSum = Vector2.Zero;
            int count = 0;
            for (int i = 0; i < _speeds.Count; i++)
            {
                if (float.IsNaN(_speeds[i])) continue;
                positionSum += _positions[i];
                headingSum += _headings[i];
                count++;
            }

            if (count == 0)
            {
                _centroid = default;
                _radius = 0f;
                _heading = default;
                return;
            }

            _centroid = positionSum / count;
            float maxExtent = 0f;
            for (int i = 0; i < _speeds.Count; i++)
            {
                if (float.IsNaN(_speeds[i])) continue;
                float extent = Vector2.Distance(_positions[i], _centroid) + _radii[i];
                if (extent > maxExtent) maxExtent = extent;
            }

            _radius = maxExtent;
            float headingLengthSq = headingSum.LengthSquared();
            _heading = headingLengthSq > 1e-8f ? headingSum / MathF.Sqrt(headingLengthSq) : Vector2.Zero;
        }

        private static bool AreEqual(float a, float b) =>
            (float.IsNaN(a) && float.IsNaN(b)) || a == b;
    }
}
