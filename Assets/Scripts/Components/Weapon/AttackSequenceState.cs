namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// One mount's salvo lifecycle: Ready → Emitting → Ready. Shots in flight never hold the mount; only the reload does.
    /// </summary>
    public sealed class AttackSequenceState
    {
        private int _generation;

        public Status CurrentStatus { get; private set; } = Status.Ready;
        public bool IsBusy => CurrentStatus == Status.Emitting;
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
