namespace EmpireAtWar.Components.Weapon
{
    public sealed class AttackSequenceState
    {
        public enum Status
        {
            Ready,
            Emitting,
            WaitingForEffects,
            Released
        }

        private int _activeEffects;
        private int _generation;

        public Status CurrentStatus { get; private set; } = Status.Ready;
        public bool IsBusy => CurrentStatus == Status.Emitting || CurrentStatus == Status.WaitingForEffects;
        public int Generation => _generation;

        public bool TryStart(out int generation)
        {
            if (CurrentStatus != Status.Ready)
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

            CurrentStatus = _activeEffects == 0 ? Status.Ready : Status.WaitingForEffects;
            if (CurrentStatus == Status.Ready)
            {
                AttackSequenceDiagnostics.RecordSequenceFinished();
            }
        }

        public bool TryCompleteEffect(int generation)
        {
            if (generation != _generation || _activeEffects == 0)
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                return false;
            }

            _activeEffects--;
            if (CurrentStatus == Status.WaitingForEffects && _activeEffects == 0)
            {
                CurrentStatus = Status.Ready;
                AttackSequenceDiagnostics.RecordSequenceFinished();
            }

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
    }
}
