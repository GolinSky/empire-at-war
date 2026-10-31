using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyCaptureObjective
    {
        public string Id { get; }
        public EnemyCaptureObjectiveType Kind { get; }
        public PlayerId Owner { get; }
        public Vector3 Position { get; }
        public float Radius { get; }
        public float Progress { get; }
        public bool IsThreatened { get; }

        public EnemyCaptureObjective(string id, EnemyCaptureObjectiveType kind, PlayerId owner,
            Vector3 position, float radius, float progress, bool isThreatened = false)
        {
            Id = id;
            Kind = kind;
            Owner = owner;
            Position = position;
            Radius = radius;
            Progress = progress;
            IsThreatened = isThreatened;
        }
    }
}
