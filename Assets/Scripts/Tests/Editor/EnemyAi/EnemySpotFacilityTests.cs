using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Round 6 (G5): a held zone gets a battle asteroid only when a known hostile ship or three hostile wings are in
    /// it, and at most one per zone; every other spot gets a mine.
    /// </summary>
    [Category("EnemyAi")]
    public sealed class EnemySpotFacilityTests
    {
        private static readonly Vector3 ZONE = new Vector3(1000f, 0f, 0f);

        private PlayerRoster _roster;
        private TeamIntelRegistry _intel;
        private SiteStub _spot;
        private SiteStub _otherSpot;
        private EnemySiteConstructionController _controller;
        private long _nextSighting = 1;

        [SetUp]
        public void SetUp()
        {
            _roster = TestPlayers.CreateDuel();
            _intel = new TeamIntelRegistry();
            _spot = new SiteStub(ZONE + new Vector3(100f, 0f, 0f));
            _otherSpot = new SiteStub(ZONE - new Vector3(100f, 0f, 0f));
            var zones = new ZoneSource();
            zones.Items.Add(new ReinforcementZone(new ZoneView(ZONE), TestPlayers.CreateLocalPlayer(_roster),
                new ReinforcementZoneModel(_roster, TestPlayers.Enemy, 10f, 1f, true)));
            _controller = new EnemySiteConstructionController(new SiteDirectory(_spot, _otherSpot), new NoPresence(),
                null, _roster.Get(TestPlayers.Enemy), null, null, null, null, zones, _intel, _roster);
        }

        [Test]
        public void HostileShipInTheHeldZone_BuildsOneBattleAsteroid_ThenMines()
        {
            Report(UnitTypeId.Ship(ShipType.Venator));

            Assert.That(_controller.ChooseSpotFacility(_spot, out _), Is.EqualTo(SiteFacilityType.BattleAsteroid));
            _otherSpot.FacilityType = SiteFacilityType.BattleAsteroid;
            _otherSpot.State = CaptureSiteState.Constructing;
            Assert.That(_controller.ChooseSpotFacility(_spot, out _), Is.EqualTo(SiteFacilityType.Mining));
        }

        [Test]
        public void HostileWingsInTheHeldZone_NeedThreeForABattleAsteroid()
        {
            for (int i = 0; i < EnemySiteConstructionController.HELD_ATTACK_WINGS - 1; i++)
                Report(UnitTypeId.Squadron(SquadronType.XWing));
            Assert.That(_controller.ChooseSpotFacility(_spot, out _), Is.EqualTo(SiteFacilityType.Mining));

            Report(UnitTypeId.Squadron(SquadronType.XWing));
            Assert.That(_controller.ChooseSpotFacility(_spot, out _), Is.EqualTo(SiteFacilityType.BattleAsteroid));
        }

        private void Report(UnitTypeId unit) =>
            _intel.Get(_roster.Get(TestPlayers.Enemy).Team).Report(new HostileSighting(_nextSighting++, unit,
                TestPlayers.Human, new FormationPoint(ZONE.x, ZONE.z), 100f, 0f, 1f, Time.time));

        private sealed class SiteStub : ICaptureSite
        {
            public SiteStub(Vector3 center) => Center = center;

            public CaptureSiteKind Kind => CaptureSiteKind.Mining;
            public int PointId => 0;
            public PlayerId Owner => TestPlayers.Enemy;
            public CaptureSiteState State { get; set; } = CaptureSiteState.Owned;
            public SiteFacilityType FacilityType { get; set; } = SiteFacilityType.Mining;
            public float CaptureProgress => 0f;
            public float ConstructionProgress => 0f;
            public Vector3 Center { get; }
            public Vector3 FacilityPosition => Center;
            public bool IsRevealed => true;
            public bool IsOperational => State == CaptureSiteState.Operational;
            public float PointerHitDistance(Ray ray) => float.PositiveInfinity;
            public bool Contains(Vector3 position, float clearance = 0f) => false;
        }

        private sealed class SiteDirectory : ICaptureSiteDirectory
        {
            public event Action SitesChanged { add { } remove { } }
            public IReadOnlyList<ICaptureSite> Sites { get; }

            public SiteDirectory(params ICaptureSite[] sites) => Sites = sites;
        }

        private sealed class NoPresence : ICaptureSitePresence
        {
            public (float Friendly, float Hostile) GetTeamStrengths(ICaptureSite site, PlayerId owner) => default;

            public bool HasHostileUnits(ICaptureSite site, PlayerId owner) => false;
        }

        private sealed class ZoneSource : IReinforcementZoneSource
        {
            public List<ReinforcementZone> Items { get; } = new List<ReinforcementZone>();
            public IReadOnlyList<ReinforcementZone> Zones => Items;
            public float SquadronCaptureWeight => 0.5f;
            public (float Friendly, float Hostile) GetTeamStrengths(ReinforcementZone zone, PlayerId owner) =>
                default;
        }

        private sealed class ZoneView : IReinforcementZoneComponent
        {
            public Vector3 Center { get; }
            public float Radius => 500f;
            public PlayerId StartingOwner => PlayerId.None;
            public bool IsCapturable => true;
            public float CaptureDuration => 10f;
            public ZoneView(Vector3 center) => Center = center;
            public void SetVisibility(bool isVisible, bool showCaptureUi) { }
            public void Render(OwnerRelation owner, OwnerRelation capturer, float captureProgress, bool isContested,
                OwnerRelation contestingFirst = OwnerRelation.Neutral, OwnerRelation contestingSecond = OwnerRelation.Neutral) { }
        }
    }
}
