using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Services.ShipNavigation
{
    public sealed class ShipDestinationRegistry
    {
        private const float IDLE_POSITION_TOLERANCE = 1f;

        private readonly Dictionary<int, Entry> _entries =
            new Dictionary<int, Entry>();

        private int _nextRegistrationId;

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
            return registrationId;
        }

        public void Unregister(int registrationId)
        {
            GetEntry(registrationId);
            _entries.Remove(registrationId);
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

            entry.PendingFinalPosition = position;
        }

        public void CancelPendingFinalPosition(int registrationId)
        {
            GetEntry(registrationId).PendingFinalPosition = null;
        }

        public bool HullsOverlap(int registrationId, ShipHullSpan hullSpan) =>
            GetEntry(registrationId).HullSpan.Overlaps(hullSpan);

        public bool IsIdle(int registrationId)
        {
            Entry entry = GetEntry(registrationId);
            if (entry.PendingFinalPosition.HasValue ||
                !entry.ActiveFinalPosition.HasValue)
            {
                return false;
            }

            FormationPoint current = entry.CurrentPosition();
            FormationPoint final = entry.ActiveFinalPosition.Value;
            float deltaX = current.X - final.X;
            float deltaZ = current.Z - final.Z;
            return deltaX * deltaX + deltaZ * deltaZ <=
                   IDLE_POSITION_TOLERANCE * IDLE_POSITION_TOLERANCE;
        }

        public void Stop(int registrationId)
        {
            Entry entry = GetEntry(registrationId);
            entry.ActiveFinalPosition = entry.CurrentPosition();
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

        private sealed class Entry
        {
            public Func<FormationPoint> CurrentPosition { get; }
            public float NavigationRadius { get; }
            public ShipHullSpan HullSpan { get; }
            public FormationPoint? ActiveFinalPosition { get; set; }
            public FormationPoint? PendingFinalPosition { get; set; }

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
            }
        }
    }
}
