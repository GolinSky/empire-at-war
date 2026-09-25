using EmpireAtWar.Entities.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Editor.Squadrons
{
    public sealed class SquadronViewSpec
    {
        public SquadronViewSpec(SquadronType type, string modelPath, int memberCount, float modelScale,
            Vector3 modelEuler, Vector3 modelOffset, float gunForward, float colliderRadius,
            Vector3[] trailOffsets, Color trailColor, string silhouettePath)
        {
            Type = type;
            ModelPath = modelPath;
            MemberCount = memberCount;
            ModelScale = modelScale;
            ModelEuler = modelEuler;
            ModelOffset = modelOffset;
            GunForward = gunForward;
            ColliderRadius = colliderRadius;
            TrailOffsets = trailOffsets;
            TrailColor = trailColor;
            SilhouettePath = silhouettePath;
        }

        public SquadronType Type { get; }
        public string ModelPath { get; }
        public int MemberCount { get; }
        public float ModelScale { get; }
        public Vector3 ModelEuler { get; }
        public Vector3 ModelOffset { get; }
        public float GunForward { get; }
        public float ColliderRadius { get; }
        public Vector3[] TrailOffsets { get; }
        public Color TrailColor { get; }
        public string SilhouettePath { get; }
    }
}
