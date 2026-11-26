using EmpireAtWar.Entities.DefendPlatform;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// The unarmed XQ-6 repair platform only supports armed platforms: never the first platform, and one per
    /// <see cref="ARMED_PLATFORMS_PER_REPAIR_PLATFORM"/> armed platforms. It is placed beside an armed platform that
    /// no own repairer covers yet.
    /// </summary>
    public static class EnemyRepairPlatformRule
    {
        public const int ARMED_PLATFORMS_PER_REPAIR_PLATFORM = 2;

        public static bool IsRepairPlatform(DefendPlatformType type) => type == DefendPlatformType.Xq6Repair;

        public static bool CanAdd(int armedPlatforms, int repairPlatforms) =>
            repairPlatforms < armedPlatforms / ARMED_PLATFORMS_PER_REPAIR_PLATFORM;
    }
}
