using EmpireAtWar.Components.Weapon;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class IncomingMissileTests
    {
        [Test]
        public void UndrawnMissile_FliesAlongItsLine_UntilAVisualReportsItsPosition()
        {
            Vector3 origin = Vector3.zero;
            Vector3 aim = new Vector3(0f, 0f, 100f);
            // Launched half a flight ago: point defense must find it halfway, even with no visual drawn.
            var missile = new IncomingMissile(null, origin, aim, Time.time - 0.5f, 1f);

            Assert.That(Vector3.Distance(missile.Position, aim * 0.5f), Is.LessThan(0.5f));

            Vector3 drawn = new Vector3(5f, 3f, 48f);
            missile.ReportPosition(drawn);
            Assert.That(missile.Position, Is.EqualTo(drawn), "A drawn visual's arc is what point defense aims at.");
        }
    }
}
