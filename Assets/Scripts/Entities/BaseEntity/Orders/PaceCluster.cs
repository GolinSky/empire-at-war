using System;
using System.Collections.Generic;
using System.Numerics;

namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    /// <summary>
    /// Shared by one spatial pace cluster of a move, attack, or attack-move order so nearby units approach together.
    /// Members report their own uncapped speed, straight-line remaining distance, position, and heading; a member
    /// that cannot move or is recovering reports <see cref="float.PositiveInfinity"/> so it never holds the cluster back.
    /// Escort members (squadrons) neither set the pace nor the mean progress while the cluster still has a ship: they
    /// fly beside the nearest ship (<see cref="TryGetEscortAnchor"/>). A cluster of escorts only paces its slowest escort.
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
        private readonly List<bool> _isEscort = new List<bool>();

        private PaceCluster _mergedInto;
        private float _issuePace;
        private int _memberCount;
        private int _anchorCount;
        private float _pace = float.PositiveInfinity;
        private float _anchorRemainingSum;
        private int _anchorRemainingCount;
        private float _escortRemainingSum;
        private int _escortRemainingCount;
        private Vector2 _centroid;
        private float _radius;
        private Vector2 _heading;
        private bool _paceDirty;
        private bool _geometryDirty;

        public int OrderId { get; }
        public PaceClusterTuning Tuning { get; }
        public bool IsMerged => _mergedInto != null;

        public int MemberCount => _mergedInto != null ? _mergedInto.MemberCount : _memberCount;

        public float IssuePace => _mergedInto != null ? _mergedInto.IssuePace : _issuePace;

        /// <summary>
        /// Slowest ship speed (slowest escort speed when no ship remains) meeting the straggler speed floor, or
        /// <see cref="float.PositiveInfinity"/> when fewer than two members remain.
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

        /// <summary>
        /// Average straight-line remaining distance of active ships (of escorts when no ship reports one), or NaN if none.
        /// </summary>
        public float MeanRemaining
        {
            get
            {
                if (_mergedInto != null) return _mergedInto.MeanRemaining;
                if (_anchorRemainingCount > 0) return _anchorRemainingSum / _anchorRemainingCount;
                return _escortRemainingCount > 0 ? _escortRemainingSum / _escortRemainingCount : float.NaN;
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

        /// <param name="referenceSpeed">
        /// The member's speed without engine damage. The straggler floor derives from the slowest reference, so a
        /// ship already damaged when the order is given cannot lower the floor and slow its cluster indefinitely.
        /// NaN uses <paramref name="speed"/>.
        /// </param>
        /// <param name="isEscort">A squadron: it escorts the cluster's ships instead of pacing them.</param>
        public int Join(float speed, float remaining = float.NaN, Vector2 position = default,
            float radius = 0f, Vector2 heading = default, float referenceSpeed = float.NaN, bool isEscort = false)
        {
            if (_mergedInto != null)
            {
                int forwarded = _mergedInto.Join(speed, remaining, position, radius, heading, referenceSpeed,
                    isEscort);
                _speeds.Add(float.NaN);
                _remaining.Add(float.NaN);
                _positions.Add(default);
                _radii.Add(0f);
                _headings.Add(default);
                _isEscort.Add(isEscort);
                _forwardedMembers.Add(forwarded);
                return _speeds.Count - 1;
            }

            float reference = float.IsNaN(referenceSpeed) ? speed : referenceSpeed;
            if (!float.IsInfinity(reference) && !float.IsNaN(reference) && reference < _issuePace)
                _issuePace = reference;

            _speeds.Add(speed);
            _remaining.Add(remaining);
            _positions.Add(position);
            _radii.Add(radius);
            _headings.Add(heading);
            _isEscort.Add(isEscort);
            _forwardedMembers.Add(-1);
            AddRemaining(remaining, 1, isEscort);
            _memberCount++;
            if (!isEscort) _anchorCount++;
            _paceDirty = true;
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
            AddRemaining(_remaining[member], -1, _isEscort[member]);
            _speeds[member] = float.NaN;
            _remaining[member] = float.NaN;
            _memberCount--;
            if (!_isEscort[member]) _anchorCount--;
            _paceDirty = true;
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
                AddRemaining(_remaining[member], -1, _isEscort[member]);
                AddRemaining(remaining, 1, _isEscort[member]);
                _remaining[member] = remaining;
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
                    source._headings[i],
                    source._issuePace,
                    source._isEscort[i]);
                source._forwardedMembers[i] = newIndex;
                source._speeds[i] = float.NaN;
                source._remaining[i] = float.NaN;
            }

            source._memberCount = 0;
            source._anchorCount = 0;
            source._pace = float.PositiveInfinity;
            source._anchorRemainingSum = 0f;
            source._anchorRemainingCount = 0;
            source._escortRemainingSum = 0f;
            source._escortRemainingCount = 0;
            source._paceDirty = false;
            source._geometryDirty = false;
            source._mergedInto = destination;
        }

        /// <summary>
        /// Lag correction for one member: ahead of the cluster's mean progress by more than the lead tolerance → pace
        /// × ahead slowdown; behind by more → uncapped to catch up; otherwise the cluster pace. Returns false once the
        /// member trailed past the straggler distance for longer than the straggler time and should detach.
        /// </summary>
        public bool TryGetSpeedCap(float remaining, float deltaTime, ref float stragglerTimer, out float cap)
        {
            cap = Pace;
            float meanRemaining = MeanRemaining;
            if (float.IsNaN(meanRemaining) || float.IsNaN(remaining)) return true;

            float behind = remaining - meanRemaining;
            // Radius costs O(n) when dirty, so it is read only for a member already past the fixed distance.
            if (behind > Tuning.StragglerDistance && behind > 3f * Radius)
            {
                stragglerTimer += deltaTime;
                if (stragglerTimer > Tuning.StragglerTime) return false;
            }
            else
            {
                stragglerTimer = 0f;
            }

            if (float.IsPositiveInfinity(cap)) return true;
            if (-behind > Tuning.LeadTolerance) cap *= Tuning.AheadSlowdown;
            else if (behind > Tuning.LeadTolerance) cap = float.PositiveInfinity;
            return true;
        }

        public PaceCluster Resolve()
        {
            PaceCluster current = this;
            while (current._mergedInto != null) current = current._mergedInto;
            return current;
        }

        /// <summary>
        /// The ship nearest to <paramref name="position"/>, which an escort flies beside; false when no ship remains.
        /// </summary>
        public bool TryGetEscortAnchor(Vector2 position, out Vector2 anchorPosition, out Vector2 anchorHeading,
            out float anchorRadius)
        {
            if (_mergedInto != null)
                return _mergedInto.TryGetEscortAnchor(position, out anchorPosition, out anchorHeading, out anchorRadius);

            anchorPosition = default;
            anchorHeading = default;
            anchorRadius = 0f;
            if (_anchorCount == 0) return false;

            float nearest = float.PositiveInfinity;
            for (int i = 0; i < _speeds.Count; i++)
            {
                if (float.IsNaN(_speeds[i]) || _isEscort[i]) continue;
                float distanceSq = Vector2.DistanceSquared(_positions[i], position);
                if (distanceSq >= nearest) continue;
                nearest = distanceSq;
                anchorPosition = _positions[i];
                anchorHeading = _headings[i];
                anchorRadius = _radii[i];
            }

            return true;
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

            // Escorts fly beside the ships, so only ships set the pace while one remains.
            bool anchorsOnly = _anchorCount > 0;
            for (int i = 0; i < _speeds.Count; i++)
            {
                float speed = _speeds[i];
                if (float.IsNaN(speed) || speed < floor || (anchorsOnly && _isEscort[i])) continue;
                if (speed < _pace) _pace = speed;
            }
        }

        // A running sum keeps MeanRemaining O(1); every member reads it every tick.
        private void AddRemaining(float remaining, int sign, bool isEscort)
        {
            if (float.IsNaN(remaining) || float.IsInfinity(remaining)) return;
            if (isEscort)
            {
                _escortRemainingSum += sign * remaining;
                _escortRemainingCount += sign;
            }
            else
            {
                _anchorRemainingSum += sign * remaining;
                _anchorRemainingCount += sign;
            }
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
