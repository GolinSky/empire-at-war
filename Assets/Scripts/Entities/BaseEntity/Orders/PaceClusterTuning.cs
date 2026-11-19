namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    /// <summary>
    /// Tuning parameters for spatial pace clustering, in-flight lag/straggler/stuck handling, and cluster merging.
    /// </summary>
    public readonly struct PaceClusterTuning
    {
        public const float LINK_DISTANCE = 600f;
        public const float LINK_RADIUS_FACTOR = 4f;
        public const float LINK_SPEED_RATIO = 2.5f;
        public const float LEAD_TOLERANCE = 150f;
        public const float AHEAD_SLOWDOWN = 0.8f;
        public const float STRAGGLER_SPEED_RATIO = 0.6f;
        public const float STRAGGLER_DISTANCE = 800f;
        public const float STRAGGLER_TIME = 8f;
        public const float STUCK_SPEED = 1f;
        public const float STUCK_DETACH_TIME = 5f;
        public const float MERGE_INTERVAL = 1f;
        public const int MERGE_HOLD_CHECKS = 2;
        public const float MERGE_FACTOR = 1.0f;
        public const float MERGE_PADDING = 100f;
        public const float MERGE_ANGLE_DEGREES = 30f;

        public static PaceClusterTuning Default => new PaceClusterTuning(
            LINK_DISTANCE,
            LINK_RADIUS_FACTOR,
            LINK_SPEED_RATIO,
            LEAD_TOLERANCE,
            AHEAD_SLOWDOWN,
            STRAGGLER_SPEED_RATIO,
            STRAGGLER_DISTANCE,
            STRAGGLER_TIME,
            STUCK_SPEED,
            STUCK_DETACH_TIME,
            MERGE_INTERVAL,
            MERGE_HOLD_CHECKS,
            MERGE_FACTOR,
            MERGE_PADDING,
            MERGE_ANGLE_DEGREES);

        public float LinkDistance { get; }
        public float LinkRadiusFactor { get; }
        public float LinkSpeedRatio { get; }
        public float LeadTolerance { get; }
        public float AheadSlowdown { get; }
        public float StragglerSpeedRatio { get; }
        public float StragglerDistance { get; }
        public float StragglerTime { get; }
        public float StuckSpeed { get; }
        public float StuckDetachTime { get; }
        public float MergeInterval { get; }
        public int MergeHoldChecks { get; }
        public float MergeFactor { get; }
        public float MergePadding { get; }
        public float MergeAngleDegrees { get; }

        public PaceClusterTuning(
            float linkDistance,
            float linkRadiusFactor,
            float linkSpeedRatio,
            float leadTolerance,
            float aheadSlowdown,
            float stragglerSpeedRatio,
            float stragglerDistance,
            float stragglerTime,
            float stuckSpeed,
            float stuckDetachTime,
            float mergeInterval,
            int mergeHoldChecks,
            float mergeFactor,
            float mergePadding,
            float mergeAngleDegrees)
        {
            LinkDistance = linkDistance;
            LinkRadiusFactor = linkRadiusFactor;
            LinkSpeedRatio = linkSpeedRatio;
            LeadTolerance = leadTolerance;
            AheadSlowdown = aheadSlowdown;
            StragglerSpeedRatio = stragglerSpeedRatio;
            StragglerDistance = stragglerDistance;
            StragglerTime = stragglerTime;
            StuckSpeed = stuckSpeed;
            StuckDetachTime = stuckDetachTime;
            MergeInterval = mergeInterval;
            MergeHoldChecks = mergeHoldChecks;
            MergeFactor = mergeFactor;
            MergePadding = mergePadding;
            MergeAngleDegrees = mergeAngleDegrees;
        }
    }
}
