using EmpireAtWar.Entities.Squadrons;
using UnityEngine;

namespace EmpireAtWar.Editor.Squadrons
{
    public sealed class SquadronViewSpec
    {
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

        public SquadronViewSpec(Vector3[] trailOffsets, string modelPath, SquadronType type,
            Vector3 modelEuler, Vector3 modelOffset, Color trailColor, float modelScale,
            float gunForward, float colliderRadius, int memberCount)
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
        }
    }
}
