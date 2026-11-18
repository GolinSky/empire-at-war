using EmpireAtWar.Services.Timing;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Timing
{
    [Category("Core")]
    public sealed class BattleSystemAttributionTests
    {
        [Test]
        public void CompletedFrame_RanksSystemsAggregatesInstancesAndSeparatesPhases()
        {
            BattleSystemAttribution.ResetFrames();
            int update = BattleSystemAttribution.Register("AttributionTest.Update.Tick()");
            int late = BattleSystemAttribution.Register("AttributionTest.Late.LateTick()");
            int fixedTick = BattleSystemAttribution.Register("AttributionTest.Fixed.FixedTick()");
            int post = BattleSystemAttribution.Register("AttributionTest.Post.PostTick()");
            BattleSystemAttribution.Record(update, 100, 20, 1000);
            BattleSystemAttribution.Record(update, 100, 30, 2000);
            BattleSystemAttribution.Record(late, 100, 70, 20);
            BattleSystemAttribution.Record(fixedTick, 100, 40, 10);
            BattleSystemAttribution.Record(post, 100, 10, 30);
            // Current frame must not leak into the completed frame.
            BattleSystemAttribution.Record(update, 101, 999, 9999);
            var frame = new BattleCaptureFrame(0, 0, 0);
            BattleSystemAttribution.ReadFrame(100, frame);
            Assert.That(frame.AllocationTop[0].Name, Is.EqualTo("AttributionTest.Update.Tick()"));
            Assert.That(frame.AllocationTop[0].Bytes, Is.EqualTo(3000));
            Assert.That(frame.AllocationTop[0].Calls, Is.EqualTo(2));
            Assert.That(frame.TimeTop[0].Name, Is.EqualTo("AttributionTest.Late.LateTick()"));
            Assert.That(frame.CoordinatorNanoseconds, Is.EqualTo(170));
            Assert.That(frame.ScriptUpdateCoordinatorNanoseconds, Is.EqualTo(60));
            BattleSystemAttribution.ReadFrame(102, frame);
            Assert.That(frame.AllocationTop[0].Name, Is.Null);
            Assert.That(frame.CoordinatorNanoseconds, Is.Zero);
        }

        [Test]
        public void RingWrapAndReset_DoNotCarryAnEarlierFramesSamples()
        {
            BattleSystemAttribution.ResetFrames();
            int id = BattleSystemAttribution.Register("AttributionTest.Wrap.Tick()");
            BattleSystemAttribution.Record(id, 300, 100, 200);
            BattleSystemAttribution.Record(id, 303, 10, 20);
            var frame = new BattleCaptureFrame(0, 0, 0);
            BattleSystemAttribution.ReadFrame(300, frame);
            Assert.That(frame.TimeTop[0].Name, Is.Null);
            BattleSystemAttribution.ReadFrame(303, frame);
            Assert.That(frame.AllocationTop[0].Bytes, Is.EqualTo(20));
            BattleSystemAttribution.ResetFrames();
            BattleSystemAttribution.ReadFrame(303, frame);
            Assert.That(frame.AllocationTop[0].Name, Is.Null);
        }
    }
}
