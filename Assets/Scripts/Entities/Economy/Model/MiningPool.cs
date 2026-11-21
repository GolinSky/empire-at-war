namespace EmpireAtWar.Models.Economy
{
    /// <summary>
    /// Mine income of a battle map: a fixed amount per player in the match, shared evenly by the map's mining spots.
    /// A team that holds the same share of the map earns the same on every map size and player count.
    /// </summary>
    public static class MiningPool
    {
        public const float PER_PLAYER = 200f;

        public static float GetSpotIncome(int playerCount, int spotCount) => PER_PLAYER * playerCount / spotCount;

        /// <summary>
        /// Survival: the defenders hold the whole ring, so it pays them what half a skirmish map pays each side twice
        /// over: <see cref="PER_PLAYER"/> × 2 per defender, shared by the ring's spots (team shares are split later).
        /// </summary>
        public static float GetSurvivalSpotIncome(int defenderCount, int spotCount) =>
            PER_PLAYER * 2f * defenderCount / spotCount;
    }
}
