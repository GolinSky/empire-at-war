using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Services.CaptureSites;
using System.Collections.Generic;
using System.Reflection;
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
        public void EmptySite_RingRemainsAvailableWithoutCaptureStatus()
        {
            Assert.That(_view.IsVisible, Is.True);
            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, 0f, 0f));
            _presenter.SetVisibility(true, true);
            Assert.That(_view.IsVisible, Is.True);
        }

        [TestCase(1f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(1f, 1f)]
        public void Capture_StatusRequiresVision_RingRemainsAvailable(float human, float enemy)
        {
            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, human, enemy));
            _presenter.SetVisibility(true, false);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.True);

            _presenter.SetVisibility(false, true);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);

            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, 0f, 0f));
            _presenter.SetVisibility(true, true);
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
        public void RestoreCapturedSite_LeavesRingStateToViewAndHidesStatus(CaptureSiteState state)
        {
            _presenter.Restore(TestPlayers.Human, state, PlayerId.None, 0f,
                SiteFacilityType.Mining, 0f);
            _presenter.SetVisibility(true, true);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);
            Assert.That(_presenter.IsRevealed, Is.True);
        }

        [Test]
        public void CaptureCompleted_KeepsEmptySiteRingAndHidesStatus()
        {
            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, 1f, 0f));
            _presenter.SetVisibility(true, false);
            Assert.That(_view.IsVisible, Is.True);

            _presenter.TickCapture(10f, TestPlayers.DuelTally(_roster, 1f, 0f));
            _presenter.SetVisibility(true, true);
            Assert.That(_presenter.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);
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
        public void RestoreUntouchedNeutralSite_KeepsRingAvailable()
        {
            _presenter.Restore(PlayerId.None, CaptureSiteState.Neutral, PlayerId.None, 0f,
                SiteFacilityType.Mining, 0f);
            _presenter.SetVisibility(true, true);
            Assert.That(_view.IsVisible, Is.True);
        }

        [Test]
        public void CaptureSystem_ExcludesOwnedAndOperationalSitesFromCaptureTargets()
        {
            CaptureSitesSystem system = CreateSystem();
            Assert.That(system.TryGetCaptureTarget(TestPlayers.Human, Vector3.right * 100, out Vector3 position), Is.True);
            Assert.That(position, Is.EqualTo(Vector3.zero));
            _presenter.Restore(TestPlayers.Human, CaptureSiteState.Owned, PlayerId.None, 0,
                SiteFacilityType.Mining, 0);
            Assert.That(system.TryGetCaptureTarget(TestPlayers.Human, Vector3.zero, out _), Is.False);
            _presenter.Restore(TestPlayers.Enemy, CaptureSiteState.Operational, PlayerId.None, 0,
                SiteFacilityType.Mining, 0);
            Assert.That(system.TryGetCaptureTarget(TestPlayers.Human, Vector3.zero, out _), Is.False);
        }

        [Test]
        public void CaptureSystem_RaidsOnlyOperationalHostileSites()
        {
            CaptureSitesSystem system = CreateSystem();
            _presenter.Restore(TestPlayers.Enemy, CaptureSiteState.Constructing, PlayerId.None, 0,
                SiteFacilityType.Mining, 0);
            Assert.That(system.TryGetRaidTarget(TestPlayers.Human, Vector3.zero, out _), Is.False);
            _presenter.Restore(TestPlayers.Enemy, CaptureSiteState.Operational, PlayerId.None, 0,
                SiteFacilityType.Mining, 0);
            Assert.That(system.TryGetRaidTarget(TestPlayers.Human, Vector3.zero, out Vector3 position), Is.True);
            Assert.That(position, Is.EqualTo(Vector3.zero));
            Assert.That(system.TryGetRaidTarget(TestPlayers.Enemy, Vector3.zero, out _), Is.False);
        }

        [Test]
        public void CaptureSystem_RaidsPreferMinesOverCloserSatellites()
        {
            CaptureSitesSystem system = CreateSystem();
            CaptureSiteData data = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            var satellite = new CaptureSitePresenter(new FakeView(_view.TooltipHover, Vector3.right * 100f),
                TestPlayers.CreateLocalPlayer(_roster), new TooltipService(new FixedClock(), new TooltipModel(),
                    new TooltipTiming(0.35f, 0.1f)), new CaptureSiteModel(_roster, 10f, 1f), data, CaptureSiteKind.Satellite);
            ((List<CaptureSitePresenter>)typeof(CaptureSitesSystem)
                .GetField("_sites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(system)).Add(satellite);
            satellite.Restore(TestPlayers.Enemy, CaptureSiteState.Operational, PlayerId.None, 0,
                SiteFacilityType.TurbolaserSatellite, 0);
            Assert.That(system.TryGetRaidTarget(TestPlayers.Human, Vector3.right * 100f, out Vector3 position), Is.True);
            Assert.That(position, Is.EqualTo(Vector3.right * 100f), "With no hostile mine, a satellite is still raidable.");

            _presenter.Restore(TestPlayers.Enemy, CaptureSiteState.Operational, PlayerId.None, 0,
                SiteFacilityType.Mining, 0);
            Assert.That(system.TryGetRaidTarget(TestPlayers.Human, Vector3.right * 100f, out position), Is.True);
            Assert.That(position, Is.EqualTo(Vector3.zero));
            satellite.Dispose();
        }

        private CaptureSitesSystem CreateSystem()
        {
            var system = new CaptureSitesSystem();
            typeof(CaptureSitesSystem).GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(system,
                AssetDatabase.LoadAssetAtPath<CaptureSiteData>("Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset"));
            typeof(CaptureSitesSystem).GetField("_playerRoster", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(system, _roster);
            var sites = (List<CaptureSitePresenter>)typeof(CaptureSitesSystem)
                .GetField("_sites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(system);
            sites.Add(_presenter);
            return system;
        }

        [TestCase(SiteFacilityType.Mining)]
        [TestCase(SiteFacilityType.BattleAsteroid)]
        public void ConstructionStartedEmitsOnlyAfterSuccessfulPaymentAndNotRestore(SiteFacilityType type)
        {
            CaptureSitesSystem system = CreateSystem();
            var registry = new EmpireAtWar.Services.Player.PlayerRegistry();
            var builder = new BuilderStub();
            registry.RegisterSiteBuilder(TestPlayers.Human, builder);
            typeof(CaptureSitesSystem).GetField("_playerRegistry", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(system, registry);
            int starts = 0;
            system.ConstructionStarted += (payer, facility) =>
            {
                Assert.That(payer, Is.EqualTo(TestPlayers.Human));
                Assert.That(facility, Is.EqualTo(type));
                Assert.That(_presenter.State, Is.EqualTo(CaptureSiteState.Constructing));
                starts++;
            };
            _presenter.Restore(TestPlayers.Human, CaptureSiteState.Constructing, PlayerId.None, 0, type, 0.5f);
            Assert.That(starts, Is.Zero);
            _presenter.Restore(TestPlayers.Human, CaptureSiteState.Owned, PlayerId.None, 0, type, 0);
            MethodInfo start = typeof(CaptureSitesSystem).GetMethod("TryStartConstruction", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(start.Invoke(system, new object[] { _presenter, type, TestPlayers.Human }), Is.False);
            Assert.That(starts, Is.Zero);
            builder.CanSpend = true;
            Assert.That(start.Invoke(system, new object[] { _presenter, type, TestPlayers.Human }), Is.True);
            Assert.That(starts, Is.EqualTo(1));
        }

        private sealed class BuilderStub : ISiteFacilityBuilder
        {
            public bool CanSpend;
            public bool CanAfford(float price) => CanSpend;
            public bool TrySpend(float price) => CanSpend;
            public long Build(SiteFacilityType facilityType, Vector3 position, System.Action onDestroyed) => 1;
        }

        private sealed class FixedClock : ITooltipClock
        {
            public float DeltaTime => 0f;
        }

        private sealed class FakeView : ICaptureSiteView
        {
            public TooltipHoverView TooltipHover { get; }
            public Vector3 Center { get; }
            public float Radius => 30f;
            public float CaptureDuration => 10f;
            public Vector3 FacilityPosition => Vector3.zero;
            public bool IsVisible { get; private set; }
            public bool ShowStatus { get; private set; }

            public FakeView(TooltipHoverView tooltipHover, Vector3 center = default)
            {
                TooltipHover = tooltipHover;
                Center = center;
            }

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
