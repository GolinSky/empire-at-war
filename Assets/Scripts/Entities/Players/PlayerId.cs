using System;

namespace EmpireAtWar.Models.Players
{
    /// <summary>
    /// Identifies one participant of a match. The default value is <see cref="None"/>,
    /// which marks neutral objects such as unclaimed zones and sites.
    /// </summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        // Stored as index + 1 so that default(PlayerId) is None instead of the first player.
        private readonly int _encodedIndex;

        public static PlayerId None => default;

        public bool IsNone => _encodedIndex == 0;

        /// <summary>Zero-based slot index; only valid when the id is not <see cref="None"/>.</summary>
        public int Index
        {
            get
            {
                if (IsNone)
                {
                    throw new InvalidOperationException("PlayerId.None has no index.");
                }

                return _encodedIndex - 1;
            }
        }

        public PlayerId(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Player index must not be negative.");
            }

            _encodedIndex = index + 1;
        }

        public bool Equals(PlayerId other) => _encodedIndex == other._encodedIndex;

        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);

        public override int GetHashCode() => _encodedIndex;

        public override string ToString() => IsNone ? "None" : $"Player{Index}";

        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);

        public static bool operator !=(PlayerId left, PlayerId right) => !left.Equals(right);
    }
}
