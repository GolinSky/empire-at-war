using System.Collections.Generic;
using EmpireAtWar.Services.Timing;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class SlowTickServiceTests
    {
        private const int TICKABLE_COUNT = 10;
        private const int FRAMES_PER_INTERVAL = 10;
        private const float FRAME_TIME = SlowTickService.INTERVAL / FRAMES_PER_INTERVAL;

        private SlowTickService _service;
        private List<CountingTickable> _tickables;

        [SetUp]
        public void SetUp()
        {
            _service = new SlowTickService();
            _tickables = new List<CountingTickable>();
            for (int i = 0; i < TICKABLE_COUNT; i++)
            {
                CountingTickable tickable = new CountingTickable();
                _tickables.Add(tickable);
                _service.Add(tickable);
            }
        }

        [Test]
        public void Advance_OverOneInterval_TicksEachOnceSpreadAcrossFrames()
        {
            for (int frame = 0; frame < FRAMES_PER_INTERVAL; frame++)
            {
                int before = TotalTicks();
                _service.Advance(FRAME_TIME);
                Assert.That(TotalTicks() - before, Is.LessThanOrEqualTo(2));
            }

            // Float rounding may leave the last one for the next frame.
            _service.Advance(FRAME_TIME);
            foreach (CountingTickable tickable in _tickables)
            {
                Assert.That(tickable.Ticks, Is.InRange(1, 2));
            }
        }

        [Test]
        public void Advance_LongFrame_TicksEachOnce()
        {
            _service.Advance(SlowTickService.INTERVAL * 5f);

            foreach (CountingTickable tickable in _tickables)
            {
                Assert.That(tickable.Ticks, Is.EqualTo(1));
            }
        }

        [Test]
        public void Remove_StopsTicking()
        {
            CountingTickable removed = _tickables[0];
            _service.Remove(removed);

            _service.Advance(SlowTickService.INTERVAL);

            Assert.That(removed.Ticks, Is.Zero);
        }

        [Test]
        public void Remove_DuringOwnTick_KeepsTickingTheRest()
        {
            RemovingTickable removing = new RemovingTickable(_service);
            _service.Add(removing);

            _service.Advance(SlowTickService.INTERVAL);
            _service.Advance(SlowTickService.INTERVAL);

            Assert.That(removing.Ticks, Is.EqualTo(1));
            foreach (CountingTickable tickable in _tickables)
            {
                Assert.That(tickable.Ticks, Is.EqualTo(2));
            }
        }

        private int TotalTicks()
        {
            int total = 0;
            foreach (CountingTickable tickable in _tickables)
            {
                total += tickable.Ticks;
            }

            return total;
        }

        private sealed class CountingTickable : ISlowTickable
        {
            public int Ticks { get; private set; }

            public void SlowTick() => Ticks++;
        }

        private sealed class RemovingTickable : ISlowTickable
        {
            private readonly ISlowTickService _service;

            public int Ticks { get; private set; }

            public RemovingTickable(ISlowTickService service)
            {
                _service = service;
            }

            public void SlowTick()
            {
                Ticks++;
                _service.Remove(this);
            }
        }
    }
}
