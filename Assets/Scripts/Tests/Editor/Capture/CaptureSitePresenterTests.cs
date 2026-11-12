using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Player;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Capture")]
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
        public void PointerInsideCaptureRadius_RequiresARevealedMeshHit()
        {
            var sites = new[] { _presenter };
            var ray = new Ray(Vector3.up * 10f, Vector3.down);
            Assert.That(_presenter.Contains(Vector3.zero), Is.True);
            _presenter.SetVisibility(true);
            Assert.That(CaptureSitePointerQuery.Find(sites, ray), Is.Null);

            _view.HitDistance = 10f;
            Assert.That(CaptureSitePointerQuery.Find(sites, ray), Is.SameAs(_presenter));
            _presenter.SetVisibility(false);
            Assert.That(CaptureSitePointerQuery.Find(sites, ray), Is.Null);
        }

        [Test]
        public void EmptySite_RingRemainsAvailableWithoutCaptureStatus()
        {
            Assert.That(_view.IsVisible, Is.True);
            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, 0f, 0f));
            _presenter.SetVisibility(true);
            Assert.That(_view.IsVisible, Is.True);
        }

        [TestCase(1f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(1f, 1f)]
        public void Capture_StatusRequiresVision_RingRemainsAvailable(float human, float enemy)
        {
            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, human, enemy));
            _presenter.SetVisibility(true);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.True);

            _presenter.SetVisibility(false);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);

            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, 0f, 0f));
            _presenter.SetVisibility(true);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);

            _presenter.SetVisibility(false);
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
            _presenter.SetVisibility(true);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);
            Assert.That(_presenter.IsRevealed, Is.True);
        }

        [Test]
        public void CaptureCompleted_KeepsEmptySiteRingAndHidesStatus()
        {
            _presenter.TickCapture(1f, TestPlayers.DuelTally(_roster, 1f, 0f));
            _presenter.SetVisibility(true);
            Assert.That(_view.IsVisible, Is.True);

            _presenter.TickCapture(10f, TestPlayers.DuelTally(_roster, 1f, 0f));
            _presenter.SetVisibility(true);
            Assert.That(_presenter.Owner, Is.EqualTo(TestPlayers.Human));
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.False);
        }

        [Test]
        public void RestoreCaptureInProgress_RevealsRing()
        {
            _presenter.Restore(PlayerId.None, CaptureSiteState.Neutral, TestPlayers.Enemy, 0.2f,
                SiteFacilityType.Mining, 0f);
            _presenter.SetVisibility(true);
            Assert.That(_view.IsVisible, Is.True);
            Assert.That(_view.ShowStatus, Is.True);
        }

        [Test]
        public void RestoreUntouchedNeutralSite_KeepsRingAvailable()
        {
            _presenter.Restore(PlayerId.None, CaptureSiteState.Neutral, PlayerId.None, 0f,
                SiteFacilityType.Mining, 0f);
            _presenter.SetVisibility(true);
            Assert.That(_view.IsVisible, Is.True);
        }

        private CaptureSitesSystem CreateSystem()
        {
            var system = _tooltipObject.AddComponent<CaptureSitesSystem>();
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
            var registry = new PlayerRegistry();
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
            Assert.That(builder.BuildCount, Is.Zero);
            builder.CanSpend = true;
            Assert.That(start.Invoke(system, new object[] { _presenter, type, TestPlayers.Human }), Is.True);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(builder.BuildCount, Is.EqualTo(1));
            Assert.That(_presenter.FacilityId, Is.EqualTo(1));
            Assert.That(_presenter.IsCapturable, Is.False);
            builder.ProgressChanged(0.5f);
            Assert.That(_presenter.ConstructionProgress, Is.EqualTo(0.5f));
            builder.Destroyed();
            Assert.That(_presenter.State, Is.EqualTo(CaptureSiteState.Neutral));
            Assert.That(_presenter.FacilityId, Is.Zero);
        }

        private sealed class BuilderStub : ISiteFacilityBuilder
        {
            public bool CanSpend;
            public int BuildCount;
            public System.Action<float> ProgressChanged;
            public System.Action Destroyed;
            public bool CanAfford(float price) => CanSpend;
            public bool TrySpend(float price) => CanSpend;
            public long Build(SiteFacilityType facilityType, Vector3 position, System.Action onDestroyed,
                System.Action<float> onConstructionProgress)
            {
                BuildCount++;
                ProgressChanged = onConstructionProgress;
                Destroyed = onDestroyed;
                return 1;
            }
        }

        private sealed class FixedClock : ITooltipClock
        {
            public float DeltaTime => 0f;
        }

        private sealed class FakeView : ICaptureSiteView
        {
            public TooltipHoverView TooltipHover { get; }
            public Vector3 Center { get; }
            public float HitDistance { get; set; } = float.PositiveInfinity;
            public float PointerHitDistance(Ray ray) => HitDistance;
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
                float constructionProgress, bool isContested,
                OwnerRelation contestingFirst = OwnerRelation.Neutral,
                OwnerRelation contestingSecond = OwnerRelation.Neutral) { }

            public void SetVisibility(bool isVisible, bool showStatus)
            {
                IsVisible = isVisible;
                ShowStatus = isVisible && showStatus;
            }
        }
    }
}
