using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>A team station or held zone, with the radius an armada must close to before it counts as near.</summary>
    public readonly struct ArmadaAsset
    {
        public Vector3 Position { get; }
        public float Radius { get; }
        public ArmadaAssetKind Kind { get; }

        public ArmadaAsset(Vector3 position, float radius, ArmadaAssetKind kind)
        {
            Position = position;
            Radius = radius;
            Kind = kind;
        }
    }
}
