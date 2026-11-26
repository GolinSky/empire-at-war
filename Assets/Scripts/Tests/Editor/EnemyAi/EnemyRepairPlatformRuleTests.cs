using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyRepairPlatformRuleTests
    {
        [Test]
        public void OnlyTheXq6RepairPlatformIsARepairPlatform()
        {
            Assert.That(EnemyRepairPlatformRule.IsRepairPlatform(DefendPlatformType.Xq6Repair), Is.True);
            Assert.That(EnemyRepairPlatformRule.IsRepairPlatform(DefendPlatformType.Xq6), Is.False);
            Assert.That(EnemyRepairPlatformRule.IsRepairPlatform(DefendPlatformType.GolanIII), Is.False);
        }

        [Test]
        public void ARepairPlatformIsNeverTheFirstPlatform()
        {
            Assert.That(EnemyRepairPlatformRule.CanAdd(0, 0), Is.False);
            Assert.That(EnemyRepairPlatformRule.CanAdd(EnemyRepairPlatformRule.ARMED_PLATFORMS_PER_REPAIR_PLATFORM - 1, 0),
                Is.False);
        }

        [Test]
        public void EachGroupOfArmedPlatformsSupportsOneRepairPlatform()
        {
            int group = EnemyRepairPlatformRule.ARMED_PLATFORMS_PER_REPAIR_PLATFORM;
            Assert.That(EnemyRepairPlatformRule.CanAdd(group, 0), Is.True);
            Assert.That(EnemyRepairPlatformRule.CanAdd(group, 1), Is.False);
            Assert.That(EnemyRepairPlatformRule.CanAdd(2 * group, 1), Is.True);
        }
    }
}
