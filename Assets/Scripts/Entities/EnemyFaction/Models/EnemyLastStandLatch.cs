namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// W48 last stand: a fleet far weaker than the enemy (smoothed fleet advantage below <see cref="ADVANTAGE"/>) while a
    /// team station is pressed, was lost or is threatened by an armada stops sending ships out and gathers at one team
    /// station. Once on, it holds at least <see cref="SECONDS"/> after the last time both conditions held.
    /// </summary>
    public sealed class EnemyLastStandLatch
    {
        public const float ADVANTAGE = 0.5f;
        public const float SECONDS = 60f;

        private float _until = float.NegativeInfinity;

        public bool IsActive { get; private set; }

        /// <param name="fleetAdvantage">Smoothed fleet advantage of the main force; NaN before its first rating.</param>
        /// <param name="stationThreatened">A team station is pressed, an own station was lost, or an armada threatens one.</param>
        /// <returns>Whether the last stand holds now.</returns>
        public bool Update(float fleetAdvantage, bool stationThreatened, float now)
        {
            if (stationThreatened && fleetAdvantage < ADVANTAGE) _until = now + SECONDS;
            IsActive = now < _until;
            return IsActive;
        }
    }
}
