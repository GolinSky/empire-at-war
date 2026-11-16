using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.StructureStatusIcons;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class StructureStatusIconSourcesTests
    {
        [Test]
        public void StartupReconstructsOnlyLocalNonOperationalRemindersAcrossAllSources()
        {
            var first = new SiteEvents(new Site { Owner = TestPlayers.Human });
            var second = new SiteEvents(
                new Site { Owner = TestPlayers.Human, State = CaptureSiteState.Constructing },
                new Site { Owner = TestPlayers.Human, State = CaptureSiteState.Operational },
                new Site { Owner = TestPlayers.Enemy });
            var relays = new RelayEvents();
            StructureStatusIconSources sources = Create(relays, first, second);
            sources.Initialize();
            Assert.That(sources.Count, Is.EqualTo(4));
            Assert.That(sources.Model.ActiveCount, Is.EqualTo(2));
            Assert.That(sources.Model.Get(0).WorldActive, Is.True);
            Assert.That(sources.Model.Get(1).WorldActive, Is.True);
            for (int i = 0; i < 4; i++) Assert.That(sources.Model.Get(i).MinimapActive, Is.False);
            sources.LateDispose();
        }

        [Test]
        public void EventsUsePreviousOwnerForDestructionAndPreserveStablePosition()
        {
            var site = new Site { Owner = TestPlayers.Human, Kind = CaptureSiteKind.Satellite };
            var events = new SiteEvents(site);
            StructureStatusIconSources sources = Create(new RelayEvents(), events);
            sources.Initialize();
            site.Owner = PlayerId.None;
            Vector3 position = new Vector3(12f, 4f, 8f);
            int revision = sources.GetRevision(0);
            events.Emit(site, TestPlayers.Human, CaptureSiteStatusTransition.Destroyed, position);
            Assert.That(sources.GetRevision(0), Is.GreaterThan(revision),
                "A fresh transition cannot inherit the previous icon's displayed frame interval.");
            Assert.That(sources.Model.Get(0).State, Is.EqualTo(StructureStatusIconState.Destroyed));
            Assert.That(sources.Model.Get(0).Kind, Is.EqualTo(StructureStatusIconKind.Satellite));
            Assert.That(sources.Model.Get(0).MinimapActive, Is.True);
            Assert.That(sources.GetPosition(0), Is.EqualTo(position));
            events.Emit(site, TestPlayers.Enemy, CaptureSiteStatusTransition.Destroyed, position);
            sources.Model.Advance(0, 1f, true, true);
            Assert.That(sources.Model.ActiveCount, Is.Zero);
            sources.LateDispose();
        }

        [Test]
        public void OwnershipLossClearsWithoutDestroyedAndBaselineDoesNotReplayBuilt()
        {
            var site = new Site { Owner = TestPlayers.Human };
            var events = new SiteEvents(site);
            StructureStatusIconSources sources = Create(new RelayEvents(), events);
            sources.Initialize();
            site.Owner = PlayerId.None;
            events.Emit(site, TestPlayers.Human, CaptureSiteStatusTransition.OwnershipLost, Vector3.zero);
            sources.Model.Advance(0, 1f, true, true);
            Assert.That(sources.Model.ActiveCount, Is.Zero);
            site.Owner = TestPlayers.Human;
            events.Emit(site, PlayerId.None, CaptureSiteStatusTransition.Captured, Vector3.zero);
            Assert.That(sources.Model.Get(0).MinimapActive, Is.True);
            site.State = CaptureSiteState.Operational;
            events.Emit(site, TestPlayers.Human, CaptureSiteStatusTransition.Baseline, Vector3.zero);
            Assert.That(sources.Model.ActiveCount, Is.Zero);
            site.State = CaptureSiteState.Constructing;
            events.Emit(site, TestPlayers.Human, CaptureSiteStatusTransition.Baseline, Vector3.zero);
            Assert.That(sources.Model.Get(0).WorldActive, Is.True);
            Assert.That(sources.Model.Get(0).MinimapActive, Is.False);
            sources.LateDispose();
        }

        [Test]
        public void SourceReplacementResetsPendingEventsAndDisposeUnsubscribesEverySource()
        {
            var first = new SiteEvents(new Site { Owner = TestPlayers.Human });
            var second = new SiteEvents(new Site { Owner = TestPlayers.Enemy });
            var relays = new RelayEvents();
            StructureStatusIconSources sources = Create(relays, first, second);
            sources.Initialize();
            sources.Model.Show(0, StructureStatusIconKind.Mining, StructureStatusIconState.Destroyed);
            int version = sources.Version;
            first.Replace(new Site { Owner = TestPlayers.Enemy });
            Assert.That(sources.Version, Is.GreaterThan(version));
            Assert.That(sources.Model.ActiveCount, Is.Zero);
            sources.LateDispose();
            Assert.That(sources.Count, Is.Zero);
            Assert.That(first.SubscriberCount, Is.Zero);
            Assert.That(second.SubscriberCount, Is.Zero);
            Assert.That(relays.SubscriberCount, Is.Zero);
        }

        [TestCase(CaptureSiteKind.Mining)]
        [TestCase(CaptureSiteKind.Satellite)]
        public void StartupAndMapReplacementUseTheSameFacilityAnchorAsLiveCapture(CaptureSiteKind kind)
        {
            var site = new Site { Owner = TestPlayers.Human, Kind = kind,
                Center = new Vector3(12f, 3f, 8f), FacilityPosition = new Vector3(12f, 15f, 8f) };
            var events = new SiteEvents(site);
            StructureStatusIconSources sources = Create(new RelayEvents(), events);
            sources.Initialize();
            Assert.That(sources.GetPosition(0), Is.EqualTo(site.FacilityPosition));
            Assert.That(sources.Model.Get(0).WorldActive, Is.True);
            Assert.That(sources.Model.Get(0).MinimapActive, Is.False);
            events.Emit(site, PlayerId.None, CaptureSiteStatusTransition.Captured, site.FacilityPosition);
            Assert.That(sources.GetPosition(0), Is.EqualTo(site.FacilityPosition));
            var replacement = new Site { Owner = TestPlayers.Human, Kind = kind,
                State = CaptureSiteState.Constructing, Center = new Vector3(-8f, 2f, 4f),
                FacilityPosition = new Vector3(-8f, 20f, 4f) };
            events.Replace(replacement);
            Assert.That(sources.GetPosition(0), Is.EqualTo(replacement.FacilityPosition));
            Assert.That(sources.Model.Get(0).WorldActive, Is.EqualTo(kind == CaptureSiteKind.Mining));
            Assert.That(sources.Model.Get(0).MinimapActive, Is.False);
            sources.LateDispose();
        }

        [TestCase(CaptureSiteKind.Mining)]
        [TestCase(CaptureSiteKind.Satellite)]
        public void ConstructionPurchaseDismissesOnlySatelliteReminderAndCompletionStillShowsBuilt(CaptureSiteKind kind)
        {
            var site = new Site { Owner = TestPlayers.Human, Kind = kind };
            var events = new SiteEvents(site);
            StructureStatusIconSources sources = Create(new RelayEvents(), events);
            sources.Initialize();
            events.Emit(site, PlayerId.None, CaptureSiteStatusTransition.Captured, Vector3.zero);
            sources.Model.Advance(0, 1f, true, true);
            site.State = CaptureSiteState.Constructing;
            events.Emit(site, TestPlayers.Human, CaptureSiteStatusTransition.ConstructionStarted, Vector3.zero);
            sources.Model.Advance(0, 1f, true, true);
            Assert.That(sources.Model.Get(0).WorldActive, Is.EqualTo(kind == CaptureSiteKind.Mining));
            Assert.That(sources.Model.Get(0).MinimapActive, Is.EqualTo(kind == CaptureSiteKind.Mining));
            site.State = CaptureSiteState.Operational;
            events.Emit(site, TestPlayers.Human, CaptureSiteStatusTransition.Built, Vector3.zero);
            Assert.That(sources.Model.Get(0).State, Is.EqualTo(StructureStatusIconState.Built));
            Assert.That(sources.Model.Get(0).WorldActive, Is.True);
            Assert.That(sources.Model.Get(0).MinimapActive, Is.True);
            sources.LateDispose();
        }

        [Test]
        public void ConstructingSatelliteBaselineAndCoalescedCaptureNeverRestorePurchaseReminder()
        {
            var site = new Site { Owner = TestPlayers.Human, Kind = CaptureSiteKind.Satellite,
                State = CaptureSiteState.Constructing };
            var events = new SiteEvents(site);
            StructureStatusIconSources sources = Create(new RelayEvents(), events);
            sources.Initialize();
            Assert.That(sources.Model.ActiveCount, Is.Zero);
            events.Emit(site, PlayerId.None, CaptureSiteStatusTransition.Captured, Vector3.zero);
            Assert.That(sources.Model.ActiveCount, Is.Zero);
            events.Emit(site, TestPlayers.Human, CaptureSiteStatusTransition.Baseline, Vector3.zero);
            Assert.That(sources.Model.ActiveCount, Is.Zero);
            sources.LateDispose();
        }

        private static StructureStatusIconSources Create(RelayEvents relays, params SiteEvents[] sites) =>
            new StructureStatusIconSources(sites, relays, TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));

        private sealed class SiteEvents : ICaptureSiteStatusEvents
        {
            public event Action SitesChanged;
            public event Action<CaptureSiteStatusChange> StatusChanged;
            public IReadOnlyList<ICaptureSite> Sites { get; private set; }
            public int SubscriberCount => (SitesChanged?.GetInvocationList().Length ?? 0) +
                                          (StatusChanged?.GetInvocationList().Length ?? 0);
            public SiteEvents(params Site[] sites) => Sites = sites;
            public void Replace(params Site[] sites) { Sites = sites; SitesChanged?.Invoke(); }
            public void Emit(Site site, PlayerId previous, CaptureSiteStatusTransition transition, Vector3 position) =>
                StatusChanged?.Invoke(new CaptureSiteStatusChange(site, position, previous, site.Owner, transition));
        }

        private sealed class RelayEvents : IRelayStatusEvents
        {
            public event Action<RelayOwnershipChange> RelayOwnershipChanged;
            public event Action RelaySourcesChanged;
            public IReadOnlyList<ReinforcementZonePresenter> Zones => Array.Empty<ReinforcementZonePresenter>();
            public int SubscriberCount => (RelayOwnershipChanged?.GetInvocationList().Length ?? 0) +
                                          (RelaySourcesChanged?.GetInvocationList().Length ?? 0);
        }

        private sealed class Site : ICaptureSite
        {
            public CaptureSiteKind Kind { get; set; } = CaptureSiteKind.Mining;
            public int PointId => 0;
            public PlayerId Owner { get; set; }
            public CaptureSiteState State { get; set; } = CaptureSiteState.Owned;
            public SiteFacilityType FacilityType => SiteFacilityType.Mining;
            public float CaptureProgress => 0f;
            public float ConstructionProgress => 0f;
            public Vector3 Center { get; set; }
            public Vector3 FacilityPosition { get; set; }
            public bool IsRevealed => false;
            public bool IsOperational => State == CaptureSiteState.Operational;
            public float PointerHitDistance(Ray ray) => throw new NotSupportedException();
            public bool Contains(Vector3 position, float clearance = 0f) => throw new NotSupportedException();
        }
    }
}
