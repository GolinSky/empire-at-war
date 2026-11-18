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

        public SquadronViewSpec(string modelPath, SquadronType type,
            Vector3 modelEuler, Vector3 modelOffset, float modelScale,
            float gunForward, int memberCount)
        {
            Type = type;
            ModelPath = modelPath;
            MemberCount = memberCount;
            ModelScale = modelScale;
            ModelEuler = modelEuler;
            ModelOffset = modelOffset;
            GunForward = gunForward;
        }
    }
}
