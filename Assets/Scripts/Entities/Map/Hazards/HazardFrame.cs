using UnityEngine;

namespace EmpireAtWar.Entities.Map.Hazards
{
    /// <summary>The frame a cloud's noise is sampled in. A mirrored copy negates the axis, so its noise mirrors
    /// with it; a folded frame reflects across its normal so a lone cloud is symmetric about that line.</summary>
    public readonly struct HazardFrame
    {
        public Vector3 Origin { get; }
        public Vector3 Along { get; }
        public bool IsFolded { get; }
        public Vector2 NoiseOffset { get; }

        public HazardFrame(Vector3 origin, Vector3 along, bool isFolded, Vector2 noiseOffset)
        {
            Origin = origin;
            Along = along;
            IsFolded = isFolded;
            NoiseOffset = noiseOffset;
        }

        /// <summary>Planar coordinates along and across the frame, before folding.</summary>
        public Vector2 ToLocal(Vector3 position)
        {
            float x = position.x - Origin.x;
            float z = position.z - Origin.z;
            return new Vector2(x * Along.x + z * Along.z, z * Along.x - x * Along.z);
        }

        public Vector3 ToWorldDirection(Vector2 local)
        {
            return new Vector3(local.x * Along.x - local.y * Along.z, 0f, local.x * Along.z + local.y * Along.x);
        }

        public HazardFrame Mirror(Vector3 pivot)
        {
            return new HazardFrame(pivot * 2f - Origin, -Along, IsFolded, NoiseOffset);
        }
    }
}
