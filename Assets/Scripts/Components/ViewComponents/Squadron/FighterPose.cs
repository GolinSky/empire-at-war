using Unity.Mathematics;

namespace EmpireAtWar.ViewComponents.Squadrons
{
    /// <summary>One fighter's flight-model pose for <see cref="FighterPoseBatch"/>; inactive slots are skipped.</summary>
    internal struct FighterPose
    {
        public float3 Position;
        public float3 Forward;
        /// <summary>Roll in degrees around the forward axis.</summary>
        public float Bank;
        public quaternion BodyRestRotation;
        public byte IsActive;
    }
}
