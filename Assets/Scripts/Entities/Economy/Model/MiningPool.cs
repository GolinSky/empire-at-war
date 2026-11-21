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
    }
}
