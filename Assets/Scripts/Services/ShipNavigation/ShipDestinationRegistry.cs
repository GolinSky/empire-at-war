using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Services.ShipNavigation
{
    public sealed class ShipDestinationRegistry
    {
        private const int MAX_SPATIAL_QUERY_VISITS = 256;
        private const int MAX_SPATIAL_QUERY_CELLS = 64;
        private readonly Dictionary<int, Entry> _entries =
            new Dictionary<int, Entry>();
        private readonly Dictionary<(int X, int Z), int> _spatialCells = new Dictionary<(int, int), int>();
        private readonly List<List<SpatialPoint>> _spatialBuckets = new List<List<SpatialPoint>>();
        private int _spatialBucketCount;
        private bool _spatialQueries;
        private bool _spatialDirty;
        private float _maximumRadius;
        private float _spatialCellSize;

        public int SpatialQuerySaturations { get; private set; }

        private int _nextRegistrationId;
        private int _version;

        // Unlike Version, this never scans live positions. Changes conservatively wake failed spacing work.
        public int ReservationVersion { get; private set; }

        public int Version
        {
            get
            {
                foreach (Entry entry in _entries.Values)
                {
                    FormationPoint current = entry.CurrentPosition();
                    if (PositionsEqual(current, entry.LastPosition)) continue;
                    entry.LastPosition = current;
                    _version++;
                }
                return _version;
            }
        }

        public int Register(
            Func<FormationPoint> currentPosition,
            float navigationRadius,
            ShipHullSpan hullSpan,
            FormationPoint initialFinalPosition)
        {
            if (currentPosition == null)
            {
                throw new ArgumentNullException(nameof(currentPosition));
            }

            // Spawn points are validated by the spawner's hull check; navigation radii may overlap.
            ValidateRadius(navigationRadius);
            int registrationId = _nextRegistrationId++;
            _entries.Add(registrationId, new Entry(
                currentPosition: currentPosition,
                navigationRadius: navigationRadius,
                hullSpan: hullSpan,
                activeFinalPosition: initialFinalPosition));
            _spatialDirty = true;
            _version++;
            ReservationVersion++;
            return registrationId;
        }

        public void Unregister(int registrationId)
        {
            GetEntry(registrationId);
            _entries.Remove(registrationId);
            _spatialDirty = true;
            _version++;
            ReservationVersion++;
        }

        public bool HasClearance(
            FormationPoint position,
            float navigationRadius)
        {
            return IsPositionClear(position, navigationRadius, ShipHullSpan.Unbounded, null);
        }

        public bool HasClearance(
            int registrationId,
            FormationPoint position,
            float navigationRadius)
        {
            Entry entry = GetEntry(registrationId);
            return IsPositionClear(position, navigationRadius, entry.HullSpan, registrationId);
        }

        public void CommitActiveFinalPosition(
            int registrationId,
            FormationPoint position,
            bool requireClearance = true)
        {
            Entry entry = GetEntry(registrationId);
            if (requireClearance && !IsPositionClear(position, entry.NavigationRadius, entry.HullSpan, registrationId))
            {
                throw new InvalidOperationException(
                    "The ship's final position is already occupied.");
            }

            if (!entry.ActiveFinalPosition.HasValue || !PositionsEqual(entry.ActiveFinalPosition.Value, position) ||
                entry.PendingFinalPosition.HasValue)
            {
                _version++;
                ReservationVersion++;
            }
            entry.ActiveFinalPosition = position;
            entry.PendingFinalPosition = null;
            _spatialDirty = true;
        }

        public void ReservePendingFinalPosition(
            int registrationId,
            FormationPoint position,
            bool requireClearance = true)
        {
            Entry entry = GetEntry(registrationId);
            if (requireClearance && !IsPositionClear(position, entry.NavigationRadius, entry.HullSpan, registrationId))
            {
                throw new InvalidOperationException(
                    "The ship's deferred final position is already occupied.");
            }

            if (!entry.PendingFinalPosition.HasValue || !PositionsEqual(entry.PendingFinalPosition.Value, position))
            {
                _version++;
                ReservationVersion++;
            }
            entry.PendingFinalPosition = position;
            _spatialDirty = true;
        }

        public void CancelPendingFinalPosition(int registrationId)
        {
            Entry entry = GetEntry(registrationId);
            if (entry.PendingFinalPosition.HasValue)
            {
                _version++;
                ReservationVersion++;
            }
            entry.PendingFinalPosition = null;
            _spatialDirty = true;
        }

        public void Stop(int registrationId)
        {
            Entry entry = GetEntry(registrationId);
            FormationPoint position = entry.CurrentPosition();
            if (!entry.ActiveFinalPosition.HasValue || !PositionsEqual(entry.ActiveFinalPosition.Value, position) ||
                entry.PendingFinalPosition.HasValue)
            {
                _version++;
                ReservationVersion++;
            }
            entry.ActiveFinalPosition = position;
            entry.PendingFinalPosition = null;
            _spatialDirty = true;
        }

        /// <summary>Snapshots occupancy for one synchronous correction plan. Do not span movement updates.</summary>
        public void BeginSpatialQueries()
        {
            if (_spatialQueries) throw new InvalidOperationException("Spatial query scope is already active.");
            _spatialQueries = true;
            _spatialDirty = true;
            SpatialQuerySaturations = 0;
        }

        public void EndSpatialQueries()
        {
            _spatialQueries = false;
            for (int i = 0; i < _spatialBucketCount; i++) _spatialBuckets[i].Clear();
            _spatialBucketCount = 0;
            _spatialCells.Clear();
        }

        private void RebuildSpatialIndex()
        {
            _spatialCells.Clear();
            for (int i = 0; i < _spatialBucketCount; i++) _spatialBuckets[i].Clear();
            _spatialBucketCount = 0;
            _maximumRadius = 1f;
            foreach (Entry entry in _entries.Values)
                _maximumRadius = Math.Max(_maximumRadius, entry.NavigationRadius);
            _spatialCellSize = _maximumRadius * 2f;
            foreach (KeyValuePair<int, Entry> pair in _entries)
            {
                Entry entry = pair.Value;
                AddSpatialPoint(pair.Key, entry, entry.CurrentPosition());
                if (entry.ActiveFinalPosition.HasValue)
                    AddSpatialPoint(pair.Key, entry, entry.ActiveFinalPosition.Value);
                if (entry.PendingFinalPosition.HasValue)
                    AddSpatialPoint(pair.Key, entry, entry.PendingFinalPosition.Value);
            }
            _spatialDirty = false;
        }

        private void AddSpatialPoint(int registrationId, Entry entry, FormationPoint position)
        {
            var cell = ((int)Math.Floor(position.X / _spatialCellSize),
                (int)Math.Floor(position.Z / _spatialCellSize));
            if (!_spatialCells.TryGetValue(cell, out int bucket))
            {
                bucket = _spatialBucketCount++;
                if (bucket == _spatialBuckets.Count) _spatialBuckets.Add(new List<SpatialPoint>());
                _spatialCells.Add(cell, bucket);
            }
            _spatialBuckets[bucket].Add(new SpatialPoint(registrationId, entry, position));
        }

        private bool IsSpatialPositionClear(FormationPoint position, float radius,
            ShipHullSpan hullSpan, int? ignoredRegistrationId)
        {
            if (_spatialDirty) RebuildSpatialIndex();
            float extent = radius + _maximumRadius;
            int minX = (int)Math.Floor((position.X - extent) / _spatialCellSize);
            int maxX = (int)Math.Floor((position.X + extent) / _spatialCellSize);
            int minZ = (int)Math.Floor((position.Z - extent) / _spatialCellSize);
            int maxZ = (int)Math.Floor((position.Z + extent) / _spatialCellSize);
            int cells = 0;
            int visits = 0;
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (++cells > MAX_SPATIAL_QUERY_CELLS)
                    {
                        SpatialQuerySaturations++;
                        return false;
                    }
                    if (!_spatialCells.TryGetValue((x, z), out int bucket)) continue;
                    List<SpatialPoint> points = _spatialBuckets[bucket];
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (++visits > MAX_SPATIAL_QUERY_VISITS)
                        {
                            SpatialQuerySaturations++;
                            return false;
                        }
                        SpatialPoint point = points[i];
                        if (point.RegistrationId == ignoredRegistrationId ||
                            !point.Entry.HullSpan.Overlaps(hullSpan)) continue;
                        if (!FormationModel.HasClearance(position, radius,
                                point.Position, point.Entry.NavigationRadius))
                            return false;
                    }
                }
            }
            return true;
        }

        private readonly struct SpatialPoint
        {
            public readonly int RegistrationId;
            public readonly Entry Entry;
            public readonly FormationPoint Position;

            public SpatialPoint(int registrationId, Entry entry, FormationPoint position)
            {
                RegistrationId = registrationId;
                Entry = entry;
                Position = position;
            }
        }

        private bool IsPositionClear(
            FormationPoint position,
            float navigationRadius,
            ShipHullSpan hullSpan,
            int? ignoredRegistrationId)
        {
            ValidateRadius(navigationRadius);
            if (_spatialQueries)
                return IsSpatialPositionClear(position, navigationRadius, hullSpan, ignoredRegistrationId);
            foreach (KeyValuePair<int, Entry> pair in _entries)
            {
                Entry entry = pair.Value;
                // Ships whose hulls cannot touch vertically pass over each other.
                if (pair.Key == ignoredRegistrationId ||
                    !entry.HullSpan.Overlaps(hullSpan))
                {
                    continue;
                }

                if (!FormationModel.HasClearance(
                        position,
                        navigationRadius,
                        entry.CurrentPosition(),
                        entry.NavigationRadius))
                {
                    return false;
                }

                if (entry.ActiveFinalPosition.HasValue &&
                    !FormationModel.HasClearance(
                        position,
                        navigationRadius,
                        entry.ActiveFinalPosition.Value,
                        entry.NavigationRadius))
                {
                    return false;
                }

                if (entry.PendingFinalPosition.HasValue &&
                    !FormationModel.HasClearance(
                        position,
                        navigationRadius,
                        entry.PendingFinalPosition.Value,
                        entry.NavigationRadius))
                {
                    return false;
                }
            }

            return true;
        }

        private Entry GetEntry(int registrationId)
        {
            if (!_entries.TryGetValue(registrationId, out Entry entry))
            {
                throw new InvalidOperationException(
                    "The ship destination registration does not exist.");
            }

            return entry;
        }

        private static void ValidateRadius(float navigationRadius)
        {
            if (navigationRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(navigationRadius));
            }
        }

        private static bool PositionsEqual(FormationPoint first, FormationPoint second) =>
            first.X == second.X && first.Z == second.Z;

        private sealed class Entry
        {
            public Func<FormationPoint> CurrentPosition { get; }
            public float NavigationRadius { get; }
            public ShipHullSpan HullSpan { get; }
            public FormationPoint? ActiveFinalPosition { get; set; }
            public FormationPoint? PendingFinalPosition { get; set; }
            public FormationPoint LastPosition { get; set; }

            public Entry(
                Func<FormationPoint> currentPosition,
                ShipHullSpan hullSpan,
                FormationPoint activeFinalPosition,
                float navigationRadius)
            {
                CurrentPosition = currentPosition;
                NavigationRadius = navigationRadius;
                HullSpan = hullSpan;
                ActiveFinalPosition = activeFinalPosition;
                LastPosition = currentPosition();
            }
        }
    }
}
