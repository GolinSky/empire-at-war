using EmpireAtWar.Entities.StructureStatusIcons;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class StructureStatusIconModelTests
    {
        private StructureStatusIconModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new StructureStatusIconModel();
            _model.Reset(3);
        }

        [TestCase(StructureStatusIconState.Captured)]
        [TestCase(StructureStatusIconState.Destroyed)]
        public void WorldAcknowledgementAccumulatesOnlyDisplayedSeconds(StructureStatusIconState state)
        {
            _model.Show(0, state);
            _model.Advance(0, 1f, true, true);
            _model.Advance(0, 20f, false, true);
            Assert.That(_model.Get(0).WorldActive, Is.True);
            Assert.That(_model.Get(0).WorldSecondsLeft, Is.EqualTo(1f));
            Assert.That(_model.Get(0).MinimapActive, Is.False);
            _model.Advance(0, 1f, true, false);
            Assert.That(_model.ActiveCount, Is.Zero);
        }

        [Test]
        public void CaptureBecomesPersistentBuildReminderWithoutReplacingMinimapCapture()
        {
            _model.Show(0, StructureStatusIconState.Captured, showBuildReminder: true);
            _model.Advance(0, 1f, true, true);
            _model.Advance(0, 0.5f, false, true);
            Assert.That(_model.Get(0).State, Is.EqualTo(StructureStatusIconState.Captured));
            _model.Advance(0, 1f, true, true);

            StructureStatusIconRecord record = _model.Get(0);
            Assert.That(record.State, Is.EqualTo(StructureStatusIconState.BuildAvailable));
            Assert.That(record.MinimapState, Is.EqualTo(StructureStatusIconState.Captured));
            Assert.That(record.MinimapAlpha, Is.EqualTo(1f));
            Assert.That(record.WorldActive, Is.True);
            Assert.That(_model.ActiveCount, Is.EqualTo(1));

            _model.Advance(0, 1f, true, true);
            Assert.That(_model.Get(0).WorldActive, Is.True);
            Assert.That(_model.Get(0).MinimapActive, Is.False);
            _model.Advance(0, 100f, true, true);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(1f));
            _model.Dismiss(0);
            _model.Advance(0, 0.31f, true, true);
            Assert.That(_model.ActiveCount, Is.Zero);
        }

        [Test]
        public void RelayCaptureAcknowledgesAfterTwoDisplayedSeconds()
        {
            _model.Show(0, StructureStatusIconState.Captured);
            _model.Advance(0, 2f, true, true);
            Assert.That(_model.Get(0).WorldActive, Is.False);
            Assert.That(_model.Get(0).MinimapActive, Is.True);
        }

        [Test]
        public void MinimapHoldsFullThreeSecondsThenFadesForPointThreeSeconds()
        {
            _model.Show(0, StructureStatusIconState.Destroyed);
            Assert.That(_model.Get(0).MinimapAlpha, Is.Zero);
            _model.Advance(0, 0.1f, false, true);
            Assert.That(_model.Get(0).MinimapAlpha, Is.EqualTo(0.5f).Within(0.00001f));
            _model.Advance(0, 0.1f, false, true);
            Assert.That(_model.Get(0).MinimapAlpha, Is.EqualTo(1f));
            _model.Advance(0, 3f, false, true);
            Assert.That(_model.Get(0).MinimapAlpha, Is.EqualTo(1f).Within(0.00001f));
            _model.Advance(0, 0.15f, false, true);
            Assert.That(_model.Get(0).MinimapAlpha, Is.EqualTo(0.5f).Within(0.00001f));
            _model.Advance(0, 0.16f, false, true);
            Assert.That(_model.Get(0).MinimapActive, Is.False);
            Assert.That(_model.Get(0).MinimapAlpha, Is.Zero);
            Assert.That(_model.Get(0).WorldSecondsLeft, Is.EqualTo(2f));
        }

        [Test]
        public void HiddenHudSuspendsBothClocks()
        {
            _model.Show(0, StructureStatusIconState.Destroyed);
            _model.Advance(0, 100f, false, false);
            Assert.That(_model.Get(0).WorldSecondsLeft, Is.EqualTo(2f));
            Assert.That(_model.Get(0).MinimapAlpha, Is.Zero);
        }

        [Test]
        public void LatestTransitionReplacesBothClocksWithoutQueuedReplay()
        {
            _model.Show(0, StructureStatusIconState.Destroyed);
            _model.Advance(0, 1f, true, true);
            _model.Show(0, StructureStatusIconState.Captured, showBuildReminder: true);
            _model.Show(0, StructureStatusIconState.Destroyed);
            Assert.That(_model.Get(0).State, Is.EqualTo(StructureStatusIconState.Destroyed));
            Assert.That(_model.Get(0).WorldSecondsLeft, Is.EqualTo(2f));
            Assert.That(_model.ActiveCount, Is.EqualTo(1));
            _model.Advance(0, 3.5f, true, true);
            Assert.That(_model.ActiveCount, Is.Zero);
        }

        [Test]
        public void RestoringCurrentReminderDoesNotReplayMinimapEvent()
        {
            _model.RestoreBuildAvailable(0);
            Assert.That(_model.Get(0).State, Is.EqualTo(StructureStatusIconState.BuildAvailable));
            Assert.That(_model.Get(0).WorldActive, Is.True);
            Assert.That(_model.Get(0).MinimapActive, Is.False);
            _model.Advance(0, 100f, true, true);
            Assert.That(_model.Get(0).WorldActive, Is.True);
        }

        [Test]
        public void BurstEventsKeepIndependentLifetimeAndReverseIterationCanRemoveAll()
        {
            for (int i = 0; i < 3; i++)
                _model.Show(i, StructureStatusIconState.Captured);
            _model.Advance(0, 1f, true, true);
            Assert.That(_model.Get(1).WorldSecondsLeft, Is.EqualTo(2f));
            for (int i = _model.ActiveCount - 1; i >= 0; i--)
                _model.Advance(_model.GetActiveSource(i), 3.5f, true, true);
            Assert.That(_model.ActiveCount, Is.Zero);
        }

        [Test]
        public void ClearingMiddleSourcePreservesOtherSourcesAndSlotCanBeReused()
        {
            for (int i = 0; i < 3; i++)
                _model.Show(i, StructureStatusIconState.Captured, showBuildReminder: true);
            _model.Clear(1);
            Assert.That(_model.ActiveCount, Is.EqualTo(2));
            Assert.That(_model.GetActiveSource(0), Is.EqualTo(0));
            Assert.That(_model.GetActiveSource(1), Is.EqualTo(2));
            _model.Show(1, StructureStatusIconState.Destroyed);
            Assert.That(_model.ActiveCount, Is.EqualTo(3));
            Assert.That(_model.Get(1).MinimapAlpha, Is.Zero);
        }

        [Test]
        public void MapResetDropsAllPreviousNotifications()
        {
            _model.Show(0, StructureStatusIconState.Captured, showBuildReminder: true);
            _model.Show(2, StructureStatusIconState.Captured);
            _model.Reset(1);
            Assert.That(_model.ActiveCount, Is.Zero);
            Assert.That(_model.Get(0).WorldActive, Is.False);
            _model.Show(0, StructureStatusIconState.Destroyed);
            Assert.That(_model.ActiveCount, Is.EqualTo(1));
            Assert.That(_model.Get(0).State, Is.EqualTo(StructureStatusIconState.Destroyed));
        }

        [TestCase(StructureStatusIconState.Captured)]
        [TestCase(StructureStatusIconState.Destroyed)]
        public void WorldIconsFadeInAndTransientIconsFadeOut(StructureStatusIconState state)
        {
            _model.Show(0, state);
            Assert.That(_model.Get(0).WorldAlpha, Is.Zero);
            _model.Advance(0, 0.1f, true, false);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(0.5f).Within(0.00001f));
            _model.Advance(0, 0.1f, true, false);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(1f));
            _model.Advance(0, 1.65f, true, false);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(0.5f).Within(0.00001f));
            _model.Advance(0, 20f, false, false);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(0.5f).Within(0.00001f));
            _model.Advance(0, 0.16f, true, false);
            Assert.That(_model.Get(0).WorldActive, Is.False);
            Assert.That(_model.Get(0).WorldAlpha, Is.Zero);
        }

        [Test]
        public void DismissFadesFromCurrentOpacityWithoutRestartingOnDuplicateEvents()
        {
            _model.Show(0, StructureStatusIconState.Captured, showBuildReminder: true);
            _model.Advance(0, 0.1f, true, true);
            _model.Dismiss(0);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(0.5f).Within(0.00001f));
            Assert.That(_model.Get(0).MinimapAlpha, Is.EqualTo(0.5f).Within(0.00001f));
            _model.Advance(0, 0.15f, true, false);
            _model.Dismiss(0);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(0.25f).Within(0.00001f));
            Assert.That(_model.Get(0).MinimapAlpha, Is.EqualTo(0.5f).Within(0.00001f));
            _model.Advance(0, 0.16f, true, false);
            Assert.That(_model.Get(0).WorldActive, Is.False);
            Assert.That(_model.Get(0).MinimapActive, Is.True);
            _model.Advance(0, 0.31f, false, true);
            Assert.That(_model.ActiveCount, Is.Zero);
        }

        [Test]
        public void NewEventResetsDismissalAndRestoredRemindersFadeIn()
        {
            _model.RestoreBuildAvailable(0);
            Assert.That(_model.Get(0).WorldAlpha, Is.Zero);
            _model.Advance(0, 0.2f, true, false);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(1f));
            _model.Dismiss(0);
            _model.Show(0, StructureStatusIconState.Destroyed);
            _model.Advance(0, 0.5f, true, true);
            Assert.That(_model.Get(0).WorldAlpha, Is.EqualTo(1f));
            Assert.That(_model.Get(0).MinimapAlpha, Is.EqualTo(1f));
            Assert.That(_model.Get(0).WorldSecondsLeft, Is.EqualTo(1.5f));
        }
    }
}
