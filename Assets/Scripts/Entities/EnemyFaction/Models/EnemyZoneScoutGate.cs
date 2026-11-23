using System.Collections.Generic;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// W68 scout first: a force is sent to a zone only once its defenders are rated, i.e. the team saw the zone within
    /// <see cref="FRESH_SECONDS"/> or holds it. Otherwise it waits: first <see cref="WING_SECONDS"/> for a scouting wing,
    /// then while the fastest ship looks alone, and after <see cref="WAIT_SECONDS"/> it commits on what it knows.
    /// </summary>
    public sealed class EnemyZoneScoutGate
    {
        public const float FRESH_SECONDS = 45f;
        public const float WING_SECONDS = 20f;
        public const float WAIT_SECONDS = 45f;

        private readonly Dictionary<string, float> _seenAt = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _waitingSince = new Dictionary<string, float>();

        public void NoteSeen(string zoneId, float now) => _seenAt[zoneId] = now;

        /// <summary>The zone is held, or the team saw it within <see cref="FRESH_SECONDS"/>.</summary>
        public bool IsRated(string zoneId, bool held, float now) =>
            held || _seenAt.TryGetValue(zoneId, out float seen) && now - seen <= FRESH_SECONDS;

        /// <param name="held">The team holds the zone.</param>
        /// <param name="shipOnTheWay">An own ship is already assigned there (a ship scout or an earlier force).</param>
        public EnemyScoutStep Step(string zoneId, bool held, bool shipOnTheWay, float now)
        {
            if (IsRated(zoneId, held, now))
            {
                _waitingSince.Remove(zoneId);
                return EnemyScoutStep.Commit;
            }
            if (!_waitingSince.TryGetValue(zoneId, out float since)) _waitingSince[zoneId] = since = now;
            float waited = now - since;
            if (waited >= WAIT_SECONDS) return EnemyScoutStep.Commit;
            return waited >= WING_SECONDS && !shipOnTheWay ? EnemyScoutStep.ShipScouts : EnemyScoutStep.WaitForWing;
        }

        /// <summary>No force waits for the zone any more: a later one starts its wait afresh.</summary>
        public void Release(string zoneId) => _waitingSince.Remove(zoneId);
    }
}
