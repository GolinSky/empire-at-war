using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    [CreateAssetMenu(menuName = "Empire At War/Unit Order Settings")]
    public sealed class UnitOrderSettings : Data
    {
        [SerializeField, Min(0f)] private float guardFollowRadius = 20f;
        [SerializeField, Min(0f)] private float guardChaseDistance = 120f;
        [Tooltip("Guarding squadrons attack enemies this close to the guarded unit, so escorts join their carrier's fight.")]
        [SerializeField, Min(0f)] private float squadronGuardEngageRadius = 800f;
        [Tooltip("Guarding and idle squadrons drop a target farther than their engage radius times this factor from the guarded unit or their post.")]
        [SerializeField, Min(1f)] private float squadronLeashFactor = 1.3f;
        [SerializeField, Min(0f)] private float huntRetargetInterval = 1f;
        [SerializeField, Min(0f)] private float stationClearance = 150f;

        [Header("Spatial Pace Clusters")]
        [SerializeField, Min(0f)] private float paceLinkDistance = PaceClusterTuning.LINK_DISTANCE;
        [SerializeField, Min(0f)] private float paceLinkRadiusFactor = PaceClusterTuning.LINK_RADIUS_FACTOR;
        [SerializeField, Min(1f)] private float paceLinkSpeedRatio = PaceClusterTuning.LINK_SPEED_RATIO;
        [SerializeField, Min(0f)] private float paceLeadTolerance = PaceClusterTuning.LEAD_TOLERANCE;
        [SerializeField, Range(0.1f, 1f)] private float paceAheadSlowdown = PaceClusterTuning.AHEAD_SLOWDOWN;
        [SerializeField, Range(0.1f, 1f)] private float paceStragglerSpeedRatio = PaceClusterTuning.STRAGGLER_SPEED_RATIO;
        [SerializeField, Min(0f)] private float paceStragglerDistance = PaceClusterTuning.STRAGGLER_DISTANCE;
        [SerializeField, Min(0f)] private float paceStragglerTime = PaceClusterTuning.STRAGGLER_TIME;
        [SerializeField, Min(0f)] private float paceStuckSpeed = PaceClusterTuning.STUCK_SPEED;
        [SerializeField, Min(0f)] private float paceStuckDetachTime = PaceClusterTuning.STUCK_DETACH_TIME;
        [SerializeField, Min(0.1f)] private float paceMergeInterval = PaceClusterTuning.MERGE_INTERVAL;
        [SerializeField, Min(1)] private int paceMergeHoldChecks = PaceClusterTuning.MERGE_HOLD_CHECKS;
        [SerializeField, Min(0f)] private float paceMergeFactor = PaceClusterTuning.MERGE_FACTOR;
        [SerializeField, Min(0f)] private float paceMergePadding = PaceClusterTuning.MERGE_PADDING;
        [SerializeField, Range(0f, 180f)] private float paceMergeAngleDegrees = PaceClusterTuning.MERGE_ANGLE_DEGREES;

        public float GuardFollowRadius => guardFollowRadius;
        public float GuardChaseDistance => guardChaseDistance;
        public float SquadronGuardEngageRadius => squadronGuardEngageRadius;
        public float SquadronLeashFactor => squadronLeashFactor;
        public float HuntRetargetInterval => huntRetargetInterval;
        public float StationClearance => stationClearance;

        public PaceClusterTuning PaceTuning => new PaceClusterTuning(
            paceLinkDistance,
            paceLinkRadiusFactor,
            paceLinkSpeedRatio,
            paceLeadTolerance,
            paceAheadSlowdown,
            paceStragglerSpeedRatio,
            paceStragglerDistance,
            paceStragglerTime,
            paceStuckSpeed,
            paceStuckDetachTime,
            paceMergeInterval,
            paceMergeHoldChecks,
            paceMergeFactor,
            paceMergePadding,
            paceMergeAngleDegrees);
    }
}
