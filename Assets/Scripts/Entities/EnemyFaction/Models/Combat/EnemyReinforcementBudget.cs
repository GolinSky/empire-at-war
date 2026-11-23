using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// What one side can still add to a fight within a time window (W63 addendum): bank plus income over the window,
    /// limited by its free unit capacity, available only once a ship bought now is on the map.
    /// </summary>
    public readonly struct EnemyReinforcementBudget
    {
        /// <summary>Income only: no bank, no capacity limit, no build delay. Used until the controller supplies its numbers.</summary>
        public static EnemyReinforcementBudget IncomeOnly => new EnemyReinforcementBudget(0f, float.PositiveInfinity, 0f);

        public float Bank { get; }

        /// <summary>Most credits the free unit capacity can still take; money beyond it buys nothing.</summary>
        public float CapacityCredits { get; }

        /// <summary>Seconds until a ship bought now is on the map: queue wait plus build time.</summary>
        public float ReadySeconds { get; }

        public EnemyReinforcementBudget(float bank, float capacityCredits, float readySeconds)
        {
            Bank = bank;
            CapacityCredits = capacityCredits;
            ReadySeconds = readySeconds;
        }

        /// <summary>Credits that reach the map within <paramref name="seconds"/> at <paramref name="incomePerSecond"/>.</summary>
        public float CreditsWithin(float incomePerSecond, float seconds)
        {
            if (seconds <= ReadySeconds) return 0f;
            return Math.Min(CapacityCredits, Bank + incomePerSecond * (seconds - ReadySeconds));
        }
    }
}
