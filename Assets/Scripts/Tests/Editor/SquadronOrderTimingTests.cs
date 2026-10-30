using System;
using EmpireAtWar.Services.Squadrons;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronOrderTimingTests
    {
        [TestCase(0.26f)]
        [TestCase(2.01f)]
        public void Hitch_BoundsWorkAndServicesEverySquadronWithoutCatchUp(float hitch)
        {
            SquadronOrderScheduler scheduler = new SquadronOrderScheduler();
            int[] calls = new int[321];
            int updates = 0;
            for (int i = 0; i < calls.Length; i++)
            {
                int index = i;
                scheduler.Register(elapsed => { calls[index]++; updates++; });
            }

            for (int frame = 0; frame < 11; frame++)
            {
                updates = 0;
                scheduler.Tick(hitch);
                Assert.That(updates, Is.InRange(1, 32));
            }
            foreach (int count in calls) Assert.That(count, Is.InRange(1, 2));
        }

        [Test]
        public void DeferredUpdate_ReceivesElapsedTimeAndUnregisteredOrdersStop()
        {
            SquadronOrderScheduler scheduler = new SquadronOrderScheduler();
            float elapsed = 0f;
            Action<float> update = delta => elapsed += delta;
            scheduler.Register(update);
            scheduler.Tick(0.5f);
            Assert.That(elapsed, Is.EqualTo(0.5f).Within(0.00001f));
            scheduler.Tick(0f);
            Assert.That(elapsed, Is.EqualTo(0.5f).Within(0.00001f), "Missed intervals must not replay.");
            scheduler.Unregister(update);
            scheduler.Tick(1f);
            Assert.That(elapsed, Is.EqualTo(0.5f).Within(0.00001f));
        }

        [Test]
        public void RemovingCurrentOrder_DoesNotSkipTheNextOrder()
        {
            SquadronOrderScheduler scheduler = new SquadronOrderScheduler();
            int nextCalls = 0;
            Action<float> first = null;
            first = delta => scheduler.Unregister(first);
            scheduler.Register(first);
            scheduler.Register(delta => nextCalls++);
            scheduler.Tick(1f);
            Assert.That(nextCalls, Is.EqualTo(1));
        }
    }
}
