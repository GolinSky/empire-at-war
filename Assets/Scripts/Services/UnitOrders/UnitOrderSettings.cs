using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    [CreateAssetMenu(menuName = "Empire At War/Unit Order Settings")]
    public sealed class UnitOrderSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float guardFollowRadius = 20f;
        [SerializeField, Min(0f)] private float guardChaseDistance = 120f;
        [Tooltip("Guarding squadrons attack enemies this close to the guarded unit, so escorts join their carrier's fight.")]
        [SerializeField, Min(0f)] private float squadronGuardEngageRadius = 800f;
        [SerializeField, Min(0f)] private float huntRetargetInterval = 1f;
        [SerializeField, Min(0f)] private float stationClearance = 150f;

        public float GuardFollowRadius => guardFollowRadius;
        public float GuardChaseDistance => guardChaseDistance;
        public float SquadronGuardEngageRadius => squadronGuardEngageRadius;
        public float HuntRetargetInterval => huntRetargetInterval;
        public float StationClearance => stationClearance;
    }
}
