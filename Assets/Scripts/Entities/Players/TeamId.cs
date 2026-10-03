using System;

namespace EmpireAtWar.Models.Players
{
    /// <summary>Players that share a team are allies; players on different teams are enemies.</summary>
    public readonly struct TeamId : IEquatable<TeamId>
    {
        public int Value { get; }

        public TeamId(int value)
        {
            Value = value;
        }

        public bool Equals(TeamId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is TeamId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"Team{Value}";

        public static bool operator ==(TeamId left, TeamId right) => left.Equals(right);

        public static bool operator !=(TeamId left, TeamId right) => !left.Equals(right);
    }
}
