using System;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UiCancelRouterTests
    {
        private FakeCancelInput _input;
        private UiCancelRouter _router;
        private int _unhandledCount;

        [SetUp]
        public void SetUp()
        {
            _input = new FakeCancelInput();
            _router = new UiCancelRouter(_input);
            _router.Initialize();
            _unhandledCount = 0;
            _router.CancelUnhandled += () => _unhandledCount++;
        }

        [TearDown]
        public void TearDown()
        {
            _router.Dispose();
        }

        [Test]
        public void Cancel_GoesToMostRecentlyFocusedHandlerOnly()
        {
            FakeHandler older = new FakeHandler(true);
            FakeHandler newer = new FakeHandler(true);
            _router.Focus(older);
            _router.Focus(newer);

            _input.Escape();

            Assert.That(newer.CancelCount, Is.EqualTo(1));
            Assert.That(older.CancelCount, Is.EqualTo(0));
            Assert.That(_unhandledCount, Is.EqualTo(0));
        }

        [Test]
        public void Cancel_FallsThroughHandlersThatDoNotConsumeIt()
        {
            FakeHandler consuming = new FakeHandler(true);
            FakeHandler passing = new FakeHandler(false);
            _router.Focus(consuming);
            _router.Focus(passing);

            _input.Escape();

            Assert.That(passing.CancelCount, Is.EqualTo(1));
            Assert.That(consuming.CancelCount, Is.EqualTo(1));
        }

        [Test]
        public void Cancel_WithoutConsumer_RaisesUnhandled()
        {
            FakeHandler handler = new FakeHandler(true);
            _router.Focus(handler);
            _router.Unfocus(handler);

            _input.Escape();

            Assert.That(handler.CancelCount, Is.EqualTo(0));
            Assert.That(_unhandledCount, Is.EqualTo(1));
        }

        [Test]
        public void Refocus_MovesHandlerToTop()
        {
            FakeHandler first = new FakeHandler(true);
            FakeHandler second = new FakeHandler(true);
            _router.Focus(first);
            _router.Focus(second);
            _router.Focus(first);

            _input.Escape();

            Assert.That(first.CancelCount, Is.EqualTo(1));
            Assert.That(second.CancelCount, Is.EqualTo(0));
        }

        private sealed class FakeCancelInput : ICancelInput
        {
            public event Action CancelPressed;
            public void Escape() => CancelPressed?.Invoke();
        }

        private sealed class FakeHandler : IUiCancelHandler
        {
            private readonly bool _consumes;

            public FakeHandler(bool consumes)
            {
                _consumes = consumes;
            }

            public int CancelCount { get; private set; }

            public bool TryCancel()
            {
                CancelCount++;
                return _consumes;
            }
        }
    }
}
