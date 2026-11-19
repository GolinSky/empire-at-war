using System.Numerics;

namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    public readonly struct PaceMemberCandidate
    {
        public Vector2 Position { get; }
        public float NavigationRadius { get; }
        public float Speed { get; }
        public bool IsSquadron { get; }

        public PaceMemberCandidate(Vector2 position, float navigationRadius, float speed, bool isSquadron)
        {
            Position = position;
            NavigationRadius = navigationRadius;
            Speed = speed;
            IsSquadron = isSquadron;
        }
    }
}
