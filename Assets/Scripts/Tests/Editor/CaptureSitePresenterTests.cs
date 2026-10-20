using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Tooltip;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class CaptureSitePresenterTests
    {
        private GameObject _tooltipObject;
        private FakeView _view;
        private PlayerRoster _roster;
        private CaptureSitePresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _tooltipObject = new GameObject("Capture site tooltip");
            _tooltipObject.SetActive(false);
            _view = new FakeView(_tooltipObject.AddComponent<TooltipHoverView>());
            _roster = TestPlayers.CreateDuel();
            CaptureSiteData data = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            Assert.That(data, Is.Not.Null);
            TooltipService tooltips = new TooltipService(new FixedClock(), new TooltipModel(),
                new TooltipTiming(0.35f, 0.1f));
            _presenter = new CaptureSitePresenter(_view, TestPlayers.CreateLocalPlayer(_roster),
                tooltips, new CaptureSiteModel(_roster, 10f, 1f), data);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            Object.DestroyImmediate(_tooltipObject);
        }

        [Test]
        public void EmptySite_RemainsHiddenEvenWhenVisibleAndHovered()
        {
            Assert.That(_view.IsVisible, Is.False);
            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, 0f, 0f));
            _presenter.SetVisibility(true, true);
            Assert.That(_view.IsVisible, Is.False);
        }

        [TestCase(1f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(1f, 1f)]
        public void FirstEntry_RevealsRingAndDepartureDoesNotHideIt(float human, float enemy)
        {
            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, human, enemy));
            _presenter.SetVisibility(true, false);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.True);

            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, 0f, 0f));
            _presenter.SetVisibility(true, false);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);

            _presenter.SetVisibility(false, true);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);
            Assert.That(_presenter.IsRevealed, Is.False);
        }

        [TestCase(CaptureSiteState.Owned)]
        [TestCase(CaptureSiteState.Constructing)]
        [TestCase(CaptureSiteState.Operational)]
        public void RestoreOwnedSite_RevealsRingWithoutRevealingStatusInFog(CaptureSiteState state)
        {
            _presenter.Restore(TestPlayers.Human, state, PlayerId.None, 0f,
                SiteFacilityType.Mining, 0f);
            _presenter.SetVisibility(false, false);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);
            Assert.That(_presenter.IsRevealed, Is.False);
        }

        [Test]
        public void RestoreCaptureInProgress_RevealsRing()
        {
            _presenter.Restore(PlayerId.None, CaptureSiteState.Neutral, TestPlayers.Enemy, 0.2f,
                SiteFacilityType.Mining, 0f);
            _presenter.SetVisibility(true, false);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.True);
        }

        [Test]
        public void RestoreUntouchedNeutralSite_KeepsRingHidden()
        {
            _presenter.Restore(PlayerId.None, CaptureSiteState.Neutral, PlayerId.None, 0f,
                SiteFacilityType.Mining, 0f);
            _presenter.SetVisibility(true, true);
            Assert.That(_view.IsVisible, Is.False);
        }

        private sealed class FixedClock : ITooltipClock
        {
            public float DeltaTime => 0f;
        }

        private sealed class FakeView : ICaptureSiteView
        {
            public TooltipHoverView TooltipHover { get; }
            public Vector3 Center => Vector3.zero;
            public float Radius => 30f;
            public float CaptureDuration => 10f;
            public Vector3 FacilityPosition => Vector3.zero;
            public bool IsVisible { get; private set; }
            public bool ShowStatus { get; private set; }

            public FakeView(TooltipHoverView tooltipHover) => TooltipHover = tooltipHover;

            public void Render(OwnerRelation owner, CaptureSiteState state, SiteFacilityType facilityType,
                string facilityName, OwnerRelation capturer, float captureProgress,
                float constructionProgress, bool isContested) { }

            public void SetVisibility(bool isVisible, bool showStatus)
            {
                IsVisible = isVisible;
                ShowStatus = isVisible && showStatus;
            }
        }
    }
}
