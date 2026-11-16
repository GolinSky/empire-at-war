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

        public static float Offset(PlayerId player, float interval) => Fraction(player) * interval;

        private static float Fraction(PlayerId player) => player.Index * GOLDEN_STEP % 1f;
    }
}
