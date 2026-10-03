using EmpireAtWar.Services.Tooltip;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class TooltipServiceTests
    {
        private TooltipModel _model;
        private TooltipService _service;
        private ManualClock _clock;
        private readonly object _source = new object();

        private float _health;

        private bool _valid;

        [SetUp]
        public void SetUp()
        {
            _model = new TooltipModel();
            _clock = new ManualClock();
            _service = new TooltipService(model: _model, timing: new TooltipTiming(0.35f, 0.1f), clock: _clock);
            _valid = true;
            _health = 100f;
        }

        [Test]
        public void HoverShowsOnlyAfterDelay()
        {
            _service.Show(Provider("ship"), default);
            Advance(0.34f);
            Assert.That(_model.IsVisible, Is.False);
            Advance(0.02f);
            Assert.That(_model.IsVisible, Is.True);
        }

        [Test]
        public void ReusingSourceAndKeyKeepsDelayAndUpdatesAnchor()
        {
            TooltipHandle first = _service.Show(Provider("ship"), default);
            Advance(0.2f);
            var anchor = new TooltipAnchor(TooltipAnchorKind.Cursor, 200f, 300f);
            TooltipHandle second = _service.Show(Provider("ship"), anchor);
            Advance(0.16f);
            Assert.That(second.Id, Is.EqualTo(first.Id));
            Assert.That(_model.IsVisible, Is.True);
            Assert.That(_model.Anchor, Is.EqualTo(anchor));
        }

        [Test]
        public void ReplacementHidesCurrentAndRestartsDelay()
        {
            _service.Show(Provider("first"), default);
            Advance(0.36f);
            _service.Show(Provider("second"), default);
            Assert.That(_model.IsVisible, Is.False);
            Advance(0.2f);
            Assert.That(_model.IsVisible, Is.False);
            Advance(0.16f);
            Assert.That(_model.Content.Title, Is.EqualTo("second"));
        }

        [Test]
        public void SameKeyFromDifferentSourcesCreatesNewOwnership()
        {
            TooltipHandle old = _service.Show(Provider("ship"), default);
            TooltipHandle current = _service.Show(new TooltipContentProvider(new object(), "ship",
                () => true, () => new TooltipContent(title: "new source")), default);
            Assert.That(current.Id, Is.Not.EqualTo(old.Id));
            _service.Hide(old);
            Advance(0.36f);
            Assert.That(_model.Content.Title, Is.EqualTo("new source"));
        }

        [Test]
        public void StaleHandleCannotHideReplacement()
        {
            TooltipHandle old = _service.Show(Provider("first"), default);
            _service.Show(Provider("second"), default);
            Advance(0.36f);
            _service.Hide(old);
            Assert.That(_model.IsVisible, Is.True);
            Assert.That(_model.Content.Title, Is.EqualTo("second"));
        }

        [Test]
        public void InvalidProviderHidesImmediatelyAndCancelsPendingRequest()
        {
            _service.Show(Provider("ship"), default);
            Advance(0.36f);
            _valid = false;
            Advance(0.01f);
            Assert.That(_model.IsVisible, Is.False);
            _valid = true;
            Advance(1f);
            Assert.That(_model.IsVisible, Is.False);
        }

        [Test]
        public void RefreshEmitsOnlyForChangedLiveContent()
        {
            int changes = 0;
            _model.ContentChanged += () => changes++;
            _service.Show(Provider("ship"), default);
            Advance(0.36f);
            Advance(0.11f);
            Assert.That(changes, Is.Zero);
            _health = 75f;
            Advance(0.05f);
            Assert.That(changes, Is.Zero);
            Advance(0.06f);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(_model.Content.Stats[0].Current, Is.EqualTo(75f));
        }

        [Test]
        public void HideAllCancelsPendingAndVisibleRequests()
        {
            _service.Show(Provider("pending"), default);
            _service.HideAll();
            Advance(1f);
            Assert.That(_model.IsVisible, Is.False);
            _service.Show(Provider("visible"), default);
            Advance(0.36f);
            _service.HideAll();
            Assert.That(_model.IsVisible, Is.False);
        }

        private TooltipContentProvider Provider(string key) => new TooltipContentProvider(_source, key,
            () => _valid, () => new TooltipContent(title: key, stats: new[] { new TooltipStat(label: "Hull", current: _health, max: 100f) }));

        private void Advance(float seconds)
        {
            _clock.DeltaTime = seconds;
            _service.Tick();
        }

        private sealed class ManualClock : ITooltipClock
        {
            public float DeltaTime { get; set; }
        }
    }
}
