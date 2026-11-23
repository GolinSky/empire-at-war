using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// Station-bought platforms by tier, read from their queue build time (W50): a long build (Golan III, Empress) is a
    /// big platform, a short one (XQ-6) a small one. Each tier has its own purchase interval; a lost platform waits the
    /// same interval before it is rebought.
    /// </summary>
    public static class EnemyPlatformTier
    {
        /// <summary>A platform whose queue build time reaches this is a big platform.</summary>
        public const float BIG_PLATFORM_MIN_BUILD_SECONDS = 30f;
        public const float SMALL_PLATFORM_INTERVAL = 20f;
        public const float BIG_PLATFORM_INTERVAL = 120f;

        public static bool IsBig(FactionData data) => data.BuildTime >= BIG_PLATFORM_MIN_BUILD_SECONDS;

        public static float GetPurchaseInterval(FactionData data) =>
            IsBig(data) ? BIG_PLATFORM_INTERVAL : SMALL_PLATFORM_INTERVAL;
    }
}
