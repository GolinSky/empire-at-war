using EmpireAtWar.Components.Squadrons.Icon;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronMarkerAnchorTests
    {
        private const float FRAME = 0.016f;

        [Test]
        public void ConstantSpeedTravelKeepsTheLagSmall()
        {
            var anchor = new SquadronMarkerAnchor();
            anchor.Reset(Vector3.zero);
            const float SPEED = 60f;
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < 200; i++)
            {
                centroid += Vector3.right * (SPEED * FRAME);
                Assert.That(anchor.Advance(centroid, FRAME), Is.False);
            }

            Assert.That((centroid - anchor.Position).magnitude, Is.LessThan(SPEED * 0.25f));
        }

        [Test]
        public void AnchorConvergesOnAStationaryCentroid()
        {
            var anchor = new SquadronMarkerAnchor();
            anchor.Reset(Vector3.zero);
            for (int i = 0; i < 200; i++) anchor.Advance(new Vector3(10f, 0f, 5f), FRAME);
            Assert.That(Vector3.Distance(anchor.Position, new Vector3(10f, 0f, 5f)), Is.LessThan(0.05f));
        }

        [Test]
        public void TeleportSnapsTheAnchor()
        {
            var anchor = new SquadronMarkerAnchor();
            anchor.Reset(Vector3.zero);
            Vector3 far = new Vector3(5000f, 0f, 0f);
            Assert.That(anchor.Advance(far, FRAME), Is.True);
            Assert.That(anchor.Position, Is.EqualTo(far));
        }
    }
}
