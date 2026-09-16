namespace EmpireAtWar.Components.Weapon
{
    public static class AttackSequenceDiagnostics
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static int Starts { get; private set; }
        public static int RejectedBusyStarts { get; private set; }
        public static int EmittedShots { get; private set; }
        public static int ScheduledImpacts { get; private set; }
        public static int AppliedImpacts { get; private set; }
        public static int CancelledImpacts { get; private set; }
        public static int ActiveSequences { get; private set; }
        public static int UnmatchedCompletions { get; private set; }
#endif

        public static void RecordStart()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Starts++;
            ActiveSequences++;
#endif
        }

        public static void RecordRejectedBusyStart()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RejectedBusyStarts++;
#endif
        }

        public static void RecordEmittedShot()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EmittedShots++;
#endif
        }

        public static void RecordScheduledImpact()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ScheduledImpacts++;
#endif
        }

        public static void RecordAppliedImpact()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AppliedImpacts++;
#endif
        }

        public static void RecordCancelledImpact()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CancelledImpacts++;
#endif
        }

        public static void RecordSequenceFinished()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ActiveSequences--;
#endif
        }

        public static void RecordUnmatchedCompletion()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnmatchedCompletions++;
#endif
        }
    }
}
