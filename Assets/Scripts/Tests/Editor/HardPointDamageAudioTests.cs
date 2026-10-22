using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HardPointDamageAudioTests
    {
        [Test]
        public void OnlyLethalDamageToAliveHardpointRaisesExplosion()
        {
            var model = new HardPointModel(HardPointType.Engines, 0);
            int explosions = 0;
            model.OnDestroyedByDamage += () => explosions++;
            model.SetHealth(100f, 1f);
            model.ApplyDamage(50f);
            Assert.That(explosions, Is.Zero);
            model.ApplyDamage(50f);
            model.ApplyDamage(50f);
            Assert.That(explosions, Is.EqualTo(1));
            model.SetHealth(100f, 1f);
            model.Restore(0f);
            model.Uninstall();
            model.SetHealth(0f, 1f);
            model.ApplyDamage(50f);
            Assert.That(explosions, Is.EqualTo(1));
            model.SetHealth(100f, 1f);
            model.ApplyDamage(100f);
            Assert.That(explosions, Is.EqualTo(2));
        }
    }
}
