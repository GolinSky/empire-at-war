using System;
using EmpireAtWar.Components.Movement.Formation;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyPatrolPoint
    {
        public string Id { get; }
        public FormationPoint Position { get; internal set; }
        public string Kind { get; internal set; }
        public float Value { get; internal set; }
        public float LastSeenTime { get; internal set; } = -1f;
        public float LastVisitStaleness { get; internal set; }
        public bool IsSector => Kind == "sector";

        public EnemyPatrolPoint(string id, FormationPoint position, string kind, float value)
        {
            Id = id;
            Position = position;
            Kind = kind;
            Value = value;
        }

        public float Staleness(float now) => LastSeenTime < 0f ? now + 60f : Math.Max(0f, now - LastSeenTime);

        public float Travel(FormationPoint origin)
        {
            float x = Position.X - origin.X;
            float z = Position.Z - origin.Z;
            return (float)Math.Sqrt(x * x + z * z);
        }

        public float Score(FormationPoint origin, float now) => Staleness(now) * Value / (1f + Travel(origin));
    }
}
