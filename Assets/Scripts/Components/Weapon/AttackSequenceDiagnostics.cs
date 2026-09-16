using System.Diagnostics;

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
        public static int PoolCreated { get; private set; }
        public static int PoolReused { get; private set; }
        public static int PoolActive { get; private set; }
        public static int PoolAvailable { get; private set; }
        public static int PoolReturned { get; private set; }
        public static int PoolRetired { get; private set; }
        public static int PoolExpansions { get; private set; }
        public static int PoolActiveHighWater { get; private set; }
        public static int OwnerlessActive { get; private set; }
        public static long CandidateVisits { get; private set; }
        public static long TargetSnapshots { get; private set; }
        public static long TargetInvalidations { get; private set; }
        public static long TargetSelectionTicks { get; private set; }
        public static long TargetSelectionAttempts { get; private set; }
        public static long BatchTargetSelectionTicks { get; private set; }
        public static long BatchTargetSelectionAttempts { get; private set; }
        public static long TargetSelectionFallbacks { get; private set; }
        public static long CandidateRebuildTicks { get; private set; }
        public static long CandidateRebuilds { get; private set; }
#endif

        public static void RecordCandidateVisit()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CandidateVisits++;
#endif
        }

        public static void RecordCandidateVisits(int count)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CandidateVisits += count;
#endif
        }

        public static void RecordTargetSnapshot()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TargetSnapshots++;
#endif
        }

        public static void RecordTargetInvalidation()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TargetInvalidations++;
#endif
        }

        public static void RecordTargetSelectionTime(long startedAt)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TargetSelectionTicks += Stopwatch.GetTimestamp() - startedAt;
            TargetSelectionAttempts++;
#endif
        }

        public static void RecordTargetSelectionBatchTime(long startedAt, int attempts)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BatchTargetSelectionTicks += Stopwatch.GetTimestamp() - startedAt;
            BatchTargetSelectionAttempts += attempts;
#endif
        }

        public static void RecordTargetSelectionFallback()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TargetSelectionFallbacks++;
#endif
        }

        public static void RecordCandidateRebuildTime(long startedAt)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CandidateRebuildTicks += Stopwatch.GetTimestamp() - startedAt;
            CandidateRebuilds++;
#endif
        }

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

        public static void RecordPoolCreated(bool expansion)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PoolCreated++;
            if (expansion)
            {
                PoolExpansions++;
            }
#endif
        }

        public static void RecordPoolReused()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PoolReused++;
#endif
        }

        public static void RecordPoolActivated()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PoolActive++;
            if (PoolActive > PoolActiveHighWater)
            {
                PoolActiveHighWater = PoolActive;
            }
#endif
        }

        public static void RecordPoolAvailable(int change)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PoolAvailable += change;
#endif
        }

        public static void RecordPoolReturned()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PoolActive--;
            PoolReturned++;
#endif
        }

        public static void RecordPoolRetired()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PoolRetired++;
#endif
        }

        public static void RecordOwnerlessActive(int change)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            OwnerlessActive += change;
#endif
        }
    }
}
