using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    [CreateAssetMenu(menuName = "Empire At War/Unit Order Settings")]
    public sealed class UnitOrderSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float guardFollowRadius = 20f;
        [SerializeField, Min(0f)] private float guardChaseDistance = 120f;
        [SerializeField, Min(0f)] private float huntRetargetInterval = 1f;
        [SerializeField, Min(0f)] private float stationClearance = 150f;

        public float GuardFollowRadius => guardFollowRadius;
        public float GuardChaseDistance => guardChaseDistance;
        public float HuntRetargetInterval => huntRetargetInterval;
        public float StationClearance => stationClearance;
    }
}
