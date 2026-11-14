namespace EmpireAtWar.Components.Weapon
{
    public sealed class AttackSequenceState
    {
        private int _activeEffects;
        private int _generation;

        public Status CurrentStatus { get; private set; } = Status.Ready;
        public bool IsBusy => CurrentStatus == Status.Emitting;
        public int Generation => _generation;

        public bool TryStart(int shotsPerSalvo, int inFlightCap, out int generation)
        {
            if (CurrentStatus != Status.Ready || _activeEffects + shotsPerSalvo > inFlightCap)
            {
                AttackSequenceDiagnostics.RecordRejectedBusyStart();
                generation = _generation;
                return false;
            }

            _generation++;
            CurrentStatus = Status.Emitting;
            AttackSequenceDiagnostics.RecordStart();
            generation = _generation;
            return true;
        }

        public bool IsEmitting(int generation) =>
            generation == _generation && CurrentStatus == Status.Emitting;

        public bool RegisterEffect(int generation)
        {
            if (!IsEmitting(generation))
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                return false;
            }

            _activeEffects++;
            return true;
        }

        public void RecordShotEmission(int generation)
        {
            if (IsEmitting(generation))
            {
                AttackSequenceDiagnostics.RecordEmittedShot();
            }
        }

        public void StopEmitting(int generation)
        {
            if (!IsEmitting(generation))
            {
                return;
            }

            CurrentStatus = Status.Ready;
            AttackSequenceDiagnostics.RecordSequenceFinished();
        }

        public bool TryCompleteEffect(int generation)
        {
            // Pool lease validation owns callback lifetime; older salvos can still be in flight.
            if (_activeEffects == 0)
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                return false;
            }

            _activeEffects--;
            return true;
        }

        public void Release()
        {
            if (CurrentStatus == Status.Released)
            {
                return;
            }

            if (IsBusy)
            {
                AttackSequenceDiagnostics.RecordSequenceFinished();
            }

            _generation++;
            _activeEffects = 0;
            CurrentStatus = Status.Released;
        }

        /// <summary>Makes a released sequence ready again; the released pool no longer delivers callbacks.</summary>
        public void Restart()
        {
            if (CurrentStatus == Status.Released)
            {
                CurrentStatus = Status.Ready;
            }
        }

        public enum Status
        {
            Ready,
            Emitting,
            Released
        }
    }
}
