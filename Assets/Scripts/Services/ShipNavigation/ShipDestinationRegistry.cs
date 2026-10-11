using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Services.ShipNavigation
{
    public sealed class ShipDestinationRegistry
    {

        private readonly Dictionary<int, Entry> _entries =
            new Dictionary<int, Entry>();

        private int _nextRegistrationId;
        private int _version;

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
            _version++;
            return registrationId;
        }

        public void Unregister(int registrationId)
        {
            GetEntry(registrationId);
            _entries.Remove(registrationId);
            _version++;
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
            FormationPoint position)
        {
            Entry entry = GetEntry(registrationId);
            if (!IsPositionClear(position, entry.NavigationRadius, entry.HullSpan, registrationId))
            {
                throw new InvalidOperationException(
                    "The ship's final position is already occupied.");
            }

            if (!entry.ActiveFinalPosition.HasValue || !PositionsEqual(entry.ActiveFinalPosition.Value, position) ||
                entry.PendingFinalPosition.HasValue) _version++;
            entry.ActiveFinalPosition = position;
            entry.PendingFinalPosition = null;
        }

        public void ReservePendingFinalPosition(
            int registrationId,
            FormationPoint position)
        {
            Entry entry = GetEntry(registrationId);
            if (!IsPositionClear(position, entry.NavigationRadius, entry.HullSpan, registrationId))
            {
                throw new InvalidOperationException(
                    "The ship's deferred final position is already occupied.");
            }

            if (!entry.PendingFinalPosition.HasValue || !PositionsEqual(entry.PendingFinalPosition.Value, position)) _version++;
            entry.PendingFinalPosition = position;
        }

        public void CancelPendingFinalPosition(int registrationId)
        {
            Entry entry = GetEntry(registrationId);
            if (entry.PendingFinalPosition.HasValue) _version++;
            entry.PendingFinalPosition = null;
        }

        public void Stop(int registrationId)
        {
            Entry entry = GetEntry(registrationId);
            FormationPoint position = entry.CurrentPosition();
            if (!entry.ActiveFinalPosition.HasValue || !PositionsEqual(entry.ActiveFinalPosition.Value, position) ||
                entry.PendingFinalPosition.HasValue) _version++;
            entry.ActiveFinalPosition = position;
            entry.PendingFinalPosition = null;
        }

        private bool IsPositionClear(
            FormationPoint position,
            float navigationRadius,
            ShipHullSpan hullSpan,
            int? ignoredRegistrationId)
        {
            ValidateRadius(navigationRadius);
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
