using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Keeps AI players' periodic work on different frames. Players on one difficulty share every interval, so
    /// after their first decision each player's timer is pushed back by its own fraction of the interval.
    /// </summary>
    internal static class EnemyAiStagger
    {
        // Golden-ratio steps spread any number of players over [0, 1).
        private const float GOLDEN_STEP = 0.618034f;
        // World changes (a squadron launched, a ship lost) reach every player at once; each reacts after its own
        // delay in [WORLD_CHANGE_DELAY, 2 * WORLD_CHANGE_DELAY), and a burst of changes yields one decision.
        private const float WORLD_CHANGE_DELAY = 0.1f;

        public static float Offset(PlayerId player, float interval) => Fraction(player) * interval;

        public static float WorldChangeDelay(PlayerId player) => WORLD_CHANGE_DELAY * (1f + Fraction(player));

        private static float Fraction(PlayerId player) => player.Index * GOLDEN_STEP % 1f;
    }
}
