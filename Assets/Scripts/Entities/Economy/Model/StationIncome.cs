namespace EmpireAtWar.Models.Economy
{
    /// <summary>Station income shared by human and AI factions: per station level, raised by research and by a
    /// living second station.</summary>
    public static class StationIncome
    {
        public const float PER_LEVEL = 30f;
        public const float SECOND_STATION_SHARE = 0.5f;

        public static float Get(int level, float researchMultiplier, bool hasSecondStation) =>
            PER_LEVEL * level * researchMultiplier * (hasSecondStation ? 1f + SECOND_STATION_SHARE : 1f);
    }
}
