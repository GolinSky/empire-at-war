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
            FormationPoint initialFinalPosition)
        {
            if (currentPosition == null)
            {
                throw new ArgumentNullException(nameof(currentPosition));
            }

            ValidateRadius(navigationRadius);
            if (!IsPositionClear(initialFinalPosition, navigationRadius, null))
            {
                throw new InvalidOperationException(
                    "The ship's initial final position is already occupied.");
            }

            int registrationId = _nextRegistrationId++;
            _entries.Add(registrationId, new Entry(
                currentPosition,
                navigationRadius,
                initialFinalPosition));
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
            return IsPositionClear(position, navigationRadius, null);
        }

        public bool HasClearance(
            int registrationId,
            FormationPoint position,
            float navigationRadius)
        {
            GetEntry(registrationId);
            return IsPositionClear(position, navigationRadius, registrationId);
        }

        public void CommitActiveFinalPosition(
            int registrationId,
            FormationPoint position)
        {
            Entry entry = GetEntry(registrationId);
            if (!IsPositionClear(position, entry.NavigationRadius, registrationId))
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
            if (!IsPositionClear(position, entry.NavigationRadius, registrationId))
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
            int? ignoredRegistrationId)
        {
            ValidateRadius(navigationRadius);
            foreach (KeyValuePair<int, Entry> pair in _entries)
            {
                if (pair.Key == ignoredRegistrationId)
                {
                    continue;
                }

                Entry entry = pair.Value;
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
            public Entry(
                Func<FormationPoint> currentPosition,
                float navigationRadius,
                FormationPoint activeFinalPosition)
            {
                CurrentPosition = currentPosition;
                NavigationRadius = navigationRadius;
                ActiveFinalPosition = activeFinalPosition;
            }

            public Func<FormationPoint> CurrentPosition { get; }
            public float NavigationRadius { get; }
            public FormationPoint? ActiveFinalPosition { get; set; }
            public FormationPoint? PendingFinalPosition { get; set; }
        }
    }
}
