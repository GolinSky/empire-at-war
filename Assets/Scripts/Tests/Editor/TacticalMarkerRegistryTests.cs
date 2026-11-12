using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.TacticalMarkers;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class TacticalMarkerRegistryTests
    {
        private TacticalMarkerSettings _settings;
        private TacticalMarkerRegistry _registry;
        private BattleClock _clock;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<TacticalMarkerSettings>();
            _clock = new BattleClock(new SkirmishStateModel());
            _registry = Create(new OfflineSession(TestPlayers.Human));
        }

        [TearDown]
        public void TearDown()
        {
            _registry.Dispose();
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [Test]
        public void InvalidIssuerAndNonFinitePointDoNotPublish()
        {
            Assert.That(_registry.TryPlace(new PlaceMarkerCommand(PlayerId.None, TacticalMarkerType.Look, Vector3.zero)), Is.False);
            Assert.That(_registry.TryPlace(new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Look,
                new Vector3(float.NaN, 0, 0))), Is.False);
            Assert.That(_registry.Markers, Is.Empty);
        }

        [Test]
        public void UnknownTargetBecomesClampedPointAndRetainsTeam()
        {
            Assert.That(_registry.TryPlace(new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Attack,
                Vector3.one * 100, 99999)), Is.True);
            TacticalMarker marker = _registry.Markers[0];
            Assert.That(marker.TargetId, Is.EqualTo(TacticalMarkerTarget.NO_TARGET));
            Assert.That(marker.Point.x, Is.EqualTo(0));
            Assert.That(marker.Point.z, Is.EqualTo(0));
            Assert.That(marker.Team, Is.EqualTo(TestPlayers.CreateTeamGame().Get(TestPlayers.Human).Team));
        }

        [Test]
        public void RepeatedMarkerRefreshesExistingIdentity()
        {
            int added = 0, updated = 0;
            _registry.Added += _ => added++;
            _registry.Updated += _ => updated++;
            PlaceMarkerCommand command = new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Look, Vector3.zero);
            _registry.TryPlace(command);
            long id = _registry.Markers[0].Id;
            _registry.TryPlace(command);
            Assert.That(_registry.Markers.Count, Is.EqualTo(1));
            Assert.That(_registry.Markers[0].Id, Is.EqualTo(id));
            Assert.That(added, Is.EqualTo(1));
            Assert.That(updated, Is.EqualTo(1));
        }

        [Test]
        public void RateLimitIsPerSender()
        {
            PlaceMarkerCommand command = new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Look, Vector3.zero);
            for (int i = 0; i < _settings.RateLimit; i++) Assert.That(_registry.TryPlace(command), Is.True);
            Assert.That(_registry.TryPlace(command), Is.False);
            Assert.That(_registry.TryPlace(new PlaceMarkerCommand(TestPlayers.Ally, TacticalMarkerType.Look, Vector3.zero)), Is.True);
        }

        [Test]
        public void CapacityReplacesOldestMarkerForThatSender()
        {
            var serialized = new UnityEditor.SerializedObject(_settings);
            serialized.FindProperty("rateLimit").intValue = _settings.MaxPerPlayer + 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TacticalMarker removed = null;
            _registry.Removed += marker => removed = marker;
            for (int i = 0; i <= _settings.MaxPerPlayer; i++)
                _registry.TryPlace(new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Look,
                    Vector3.up * (i * (_settings.RepeatRadius + 1))));
            Assert.That(_registry.Markers.Count, Is.EqualTo(_settings.MaxPerPlayer));
            Assert.That(removed, Is.Not.Null);
            Assert.That(removed.State, Is.EqualTo(TacticalMarkerState.Replaced));
            Assert.That(_registry.Markers, Has.No.Member(removed));
        }

        [Test]
        public void HiddenTargetDoesNotAttachOrPublishItsPosition()
        {
            _registry.Dispose();
            HiddenVision vision = new HiddenVision();
            SiteDirectory sites = new SiteDirectory();
            _registry = new TacticalMarkerRegistry(_settings, TestPlayers.CreateTeamGame(),
                new TacticalMarkerTargets(new EntityLocator(), new[] { sites }, vision), new Map(),
                new OfflineSession(TestPlayers.Human), _clock, new PlayerCommandHandlers(), new SkirmishStateModel());
            _registry.TryPlace(new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Look,
                Vector3.zero, TacticalMarkerTarget.SiteId(0)));
            Assert.That(_registry.Markers[0].TargetId, Is.EqualTo(TacticalMarkerTarget.NO_TARGET));
            Assert.That(_registry.Markers[0].Point, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void SiteTargetsDistinguishSitesSharingTheSameMapPoint()
        {
            var mining = new Site();
            var secondMine = new Site();
            var satellite = new Site { Kind = CaptureSiteKind.Satellite };
            var sites = new SiteDirectory(mining, secondMine, satellite);
            var targets = new TacticalMarkerTargets(new EntityLocator(), new[] { sites }, new HiddenVision());

            Assert.That(mining.PointId, Is.EqualTo(satellite.PointId));
            Assert.That(targets.GetSiteId(mining), Is.Not.EqualTo(targets.GetSiteId(secondMine)));
            Assert.That(targets.GetSiteId(mining), Is.Not.EqualTo(targets.GetSiteId(satellite)));
            Assert.That(targets.FindSite(targets.GetSiteId(mining)), Is.SameAs(mining));
            Assert.That(targets.FindSite(targets.GetSiteId(secondMine)), Is.SameAs(secondMine));
            Assert.That(targets.FindSite(targets.GetSiteId(satellite)), Is.SameAs(satellite));
        }

        [Test]
        public void PreviouslyVisibleTargetFreezesWithoutPublishingHiddenMovement()
        {
            _registry.Dispose();
            HiddenVision vision = new HiddenVision { Visible = true };
            SiteDirectory sites = new SiteDirectory();
            _registry = new TacticalMarkerRegistry(_settings, TestPlayers.CreateTeamGame(),
                new TacticalMarkerTargets(new EntityLocator(), new[] { sites }, vision), new Map(),
                new OfflineSession(TestPlayers.Human), _clock, new PlayerCommandHandlers(), new SkirmishStateModel());
            _registry.TryPlace(new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Look,
                Vector3.zero, TacticalMarkerTarget.SiteId(0)));
            Vector3 lastKnown = _registry.Markers[0].Point;
            int updates = 0;
            _registry.Updated += _ => updates++;
            vision.Visible = false;
            sites.Site.Center = Vector3.one * 200;
            _registry.Tick();
            Assert.That(_registry.Markers[0].Point, Is.EqualTo(lastKnown));
            Assert.That(updates, Is.Zero);
        }

        [Test]
        public void RestoreClearsMarkersAndRateWindow()
        {
            PlaceMarkerCommand command = new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Look, Vector3.zero);
            _registry.TryPlace(command);
            _clock.Restore(12);
            Assert.That(_registry.Markers, Is.Empty);
            Assert.That(_registry.TryPlace(command), Is.True);
        }

        [Test]
        public void BattleEndClearsMarkersAndRejectsFurtherAuthorityCommands()
        {
            var command = new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Look, Vector3.zero);
            _registry.TryPlace(command);

            _registry.UpdateState(BattleState.Ended);

            Assert.That(_registry.Markers, Is.Empty);
            Assert.That(_registry.TryPlace(command), Is.False);
            _registry.UpdateState(BattleState.Loading);
            Assert.That(_registry.TryPlace(command), Is.True);
        }

        [Test]
        public void BattleEndRejectsLateClientMirrorPackets()
        {
            _registry.Dispose();
            _registry = Create(new ClientSession());
            _registry.ApplyMirror(1, TacticalMarkerType.Look, TestPlayers.Ally, Vector3.zero, -1, 0, 5);

            _registry.UpdateState(BattleState.Ended);
            _registry.ApplyMirror(2, TacticalMarkerType.Look, TestPlayers.Ally, Vector3.zero, -1, 0, 5);

            Assert.That(_registry.Markers, Is.Empty);
        }

        [Test]
        public void MirrorRejectsHostileSenderAndExpiresWithoutTrackingTargets()
        {
            _registry.Dispose();
            _registry = Create(new ClientSession());
            _registry.ApplyMirror(1, TacticalMarkerType.Attack, TestPlayers.Enemy, Vector3.one, 20, 0, 1);
            Assert.That(_registry.Markers, Is.Empty);
            _registry.ApplyMirror(2, TacticalMarkerType.Attack, TestPlayers.Ally, Vector3.one, 20, 0, 1);
            _registry.Tick();
            Assert.That(_registry.Markers[0].TargetId, Is.EqualTo(20));
            Assert.That(_registry.Markers[0].Point, Is.EqualTo(Vector3.one));
            _registry.ApplyMirror(2, TacticalMarkerType.Attack, TestPlayers.Ally, Vector3.one, 20, 0, 0);
            _registry.Tick();
            Assert.That(_registry.Markers, Is.Empty);
        }

        private TacticalMarkerRegistry Create(IMatchSession session) => new TacticalMarkerRegistry(_settings,
            TestPlayers.CreateTeamGame(), new TacticalMarkerTargets(new EntityLocator(),
                Array.Empty<ICaptureSiteDirectory>(), new HiddenVision()), new Map(), session, _clock,
                new PlayerCommandHandlers(), new SkirmishStateModel());

        private sealed class Map : IMapModelObserver
        {
            public Vector2Range SizeRange { get; } = new Vector2Range();
            public Vector3 GetStationPosition(PlayerId owner) => Vector3.zero;
        }

        private sealed class HiddenVision : IVisionService
        {
            public bool Visible;
            public bool FogEnabled => true;
            public IReadOnlyList<OwnedCircle> Sources => Array.Empty<OwnedCircle>();
            public void Register(PlayerId owner, Transform source, float radius) { }
            public void Unregister(Transform source) { }
            public bool IsVisible(PlayerId viewer, Vector3 position) => Visible;
            public bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius) => Visible;
        }

        private sealed class SiteDirectory : ICaptureSiteDirectory
        {
            public Site Site => (Site)Sites[0];
            public event Action SitesChanged { add { } remove { } }
            public IReadOnlyList<ICaptureSite> Sites { get; }
            public SiteDirectory(params Site[] sites) => Sites = sites.Length == 0 ? new[] { new Site() } : sites;
        }

        private sealed class Site : ICaptureSite
        {
            public CaptureSiteKind Kind { get; set; }
            public int PointId => 7;
            public PlayerId Owner => TestPlayers.Enemy;
            public CaptureSiteState State => default;
            public SiteFacilityType FacilityType => default;
            public float CaptureProgress => 0;
            public float ConstructionProgress => 0;
            public Vector3 Center { get; set; } = new Vector3(0, 10, 0);
            public bool IsRevealed => true;
            public bool IsOperational => true;
            public float PointerHitDistance(Ray ray) => 0;
            public bool Contains(Vector3 position, float clearance = 0) => false;
        }

        private sealed class ClientSession : IMatchSession
        {
            public bool IsOnline => true;
            public bool IsAuthority => false;
            public PlayerId LocalPlayerId => TestPlayers.Human;
        }
    }
}
