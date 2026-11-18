using System.Collections.Generic;

namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    /// <summary>
    /// Shared by the ships of one group attack order so they approach at the slowest member's speed and arrive
    /// together. Members report their own uncapped speed; a member that cannot move reports
    /// <see cref="float.PositiveInfinity"/> so it never holds the group back.
    /// </summary>
    public sealed class GroupPace
    {
        private const int MINIMUM_GROUP = 2;

        // Member id → reported speed; NaN once the member left.
        private readonly List<float> _speeds = new List<float>();

        private int _memberCount;
        private float _pace = float.PositiveInfinity;
        private bool _isDirty;

        /// <summary>The slowest member speed, or <see cref="float.PositiveInfinity"/> when fewer than two members remain.</summary>
        public float Pace
        {
            get
            {
                if (_isDirty) Recompute();
                return _pace;
            }
        }

        public int Join(float speed)
        {
            _speeds.Add(speed);
            _memberCount++;
            _isDirty = true;
            return _speeds.Count - 1;
        }

        public void Leave(int member)
        {
            _speeds[member] = float.NaN;
            _memberCount--;
            _isDirty = true;
        }

        public void Report(int member, float speed)
        {
            if (_speeds[member] == speed) return;
            _speeds[member] = speed;
            _isDirty = true;
        }

        private void Recompute()
        {
            _isDirty = false;
            _pace = float.PositiveInfinity;
            if (_memberCount < MINIMUM_GROUP) return;
            foreach (float speed in _speeds)
                if (speed < _pace) _pace = speed;
        }
    }
}
