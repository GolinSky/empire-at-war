using Unity.Mathematics;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Where a flying bolt sits this frame: <c>Start + Path * Progress</c>, facing along <c>Path</c>.</summary>
    internal struct BoltPose
    {
        public float3 Start;
        public float3 Path;
        public float Progress;
    }
}
