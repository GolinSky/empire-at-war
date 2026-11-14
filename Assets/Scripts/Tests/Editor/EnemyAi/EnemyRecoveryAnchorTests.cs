using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyRecoveryAnchorTests
    {
        private PlayerRoster _roster;
        private PlayerSlot _owner;
        private TeamIntelRegistry _intel;
        private ZoneSource _zones;
        private EnemyRecoveryAnchor _recovery;

        [SetUp]
        public void SetUp()
        {
            _roster = TestPlayers.CreateTeamGame();
            _owner = _roster.Get(TestPlayers.Ally);
            _intel = new TeamIntelRegistry();
            _zones = new ZoneSource();
            _recovery = new EnemyRecoveryAnchor(null, _zones, _roster, new EntityLocator(),
                new BattleAreaService(_roster), new Map(), _intel, _owner, null,
                new EnemyStationPressureTracker());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LostStationAcceptsHumanOrAiAllyEvenWithoutShips(bool ai)
        {
            PlayerSlot original = _roster.Get(TestPlayers.Human);
            var players = new List<PlayerSlot>(_roster.Players);
            players[0] = new PlayerSlot(original.Id, original.Team, original.Faction,
                ai ? PlayerController.Ai : PlayerController.Human, original.Difficulty, original.ColorIndex);
            _roster.Replace(players);
            AddZone(TestPlayers.Human, new Vector3(1000f, 0f, 0f));
            Update(canCapture: false);
            Assert.That(_recovery.TryGetBuildAnchor(out Vector3 anchor), Is.True);
            Assert.That(anchor.x, Is.EqualTo(1000f));
        }

        [Test]
        public void ContestedAllyUsesRecentlySeenSector()
        {
            ReinforcementZonePresenter zone = AddZone(TestPlayers.Human, new Vector3(1000f, 0f, 0f));
            zone.Restore(TestPlayers.Human, PlayerId.None, 0f, true);
            EnemyPatrolMemory memory = _intel.GetPatrol(_owner.Team);
            memory.Register("sector:1,1", new FormationPoint(1500f, 500f), "sector", 1f);
            memory.Observe("sector:1,1", Time.time);
            Update();
            Assert.That(_recovery.TryGetBuildAnchor(out Vector3 anchor), Is.True);
            Assert.That(anchor, Is.EqualTo(new Vector3(1500f, 0f, 500f)));
        }

        [Test]
        public void NeutralRelayRequiresCaptureBeforePlacement()
        {
            ReinforcementZonePresenter zone = AddZone(PlayerId.None, new Vector3(1000f, 0f, 0f));
            Update();
            Assert.That(_recovery.PendingCaptureObjective.Id, Is.EqualTo("zone:0"));
            Assert.That(_recovery.TryGetBuildAnchor(out _), Is.False);
            Assert.That(_recovery.DeferredReason, Is.EqualTo("anchorNotOwned"));
            zone.Restore(_owner.Id, PlayerId.None, 0f, false);
            Update();
            Assert.That(_recovery.TryGetBuildAnchor(out _), Is.True);
        }

        [Test]
        public void NoShipsCannotChooseNeutralRelay()
        {
            AddZone(PlayerId.None, new Vector3(1000f, 0f, 0f));
            Update(canCapture: false);
            Assert.That(_recovery.PendingCaptureObjective, Is.Null);
            Assert.That(_recovery.TryGetBuildAnchor(out _), Is.False);
        }

        [Test]
        public void StalledRelayIsExcludedUntilRetryTime()
        {
            AddZone(PlayerId.None, Vector3.right * 1000f);
            Update();
            Assert.That(_recovery.PendingCaptureObjective.Id, Is.EqualTo("zone:0"));
            _recovery.RejectCaptureObjective("zone:0", Time.time + 20f);
            Update();
            Assert.That(_recovery.PendingCaptureObjective, Is.Null);
            _recovery.Update(Vector3.zero, 0.5f, false, false, Time.time + 21f, (position, radius) => true);
            Assert.That(_recovery.PendingCaptureObjective.Id, Is.EqualTo("zone:0"));
        }

        [Test]
        public void PlacementRechecksZoneOwnershipAfterAnchorSelection()
        {
            ReinforcementZonePresenter zone = AddZone(TestPlayers.Human, new Vector3(1000f, 0f, 0f));
            Update();
            Assert.That(_recovery.TryGetBuildAnchor(out _), Is.True);
            zone.Restore(TestPlayers.Enemy, PlayerId.None, 0f, false);
            Assert.That(_recovery.TryGetBuildAnchor(out _), Is.False);
        }

        [Test]
        public void LostHomeAndStaleEmptySectorsCannotBecomeBuildAnchors()
        {
            AddZone(_owner.Id, new Vector3(-2000f, 0f, 0f));
            EnemyPatrolMemory memory = _intel.GetPatrol(_owner.Team);
            memory.Register("sector:1,1", new FormationPoint(1500f, 500f), "sector", 1f);
            Update();
            Assert.That(_recovery.TryGetBuildAnchor(out _), Is.False);
            Assert.That(_recovery.IsPlacementSafe(new Vector3(-2000f, 0f, 0f), 10f), Is.False);
        }

        [TestCase(1f, 0f)]
        [TestCase(0.1f, 2000f)]
        public void ParityPrefersFleetCoverWhileWeakFleetPrefersThreatDistance(float advantage, float expectedX)
        {
            AddZone(TestPlayers.Human, Vector3.zero);
            AddZone(TestPlayers.Human, Vector3.right * 2000f);
            ReportHostile(new FormationPoint(-1000f, 0f));
            _recovery.Update(Vector3.zero, advantage, false, false, Time.time, (position, radius) => true);
            Assert.That(_recovery.TryGetBuildAnchor(out Vector3 anchor), Is.True);
            Assert.That(anchor.x, Is.EqualTo(expectedX));
        }

        [Test]
        public void HostileReachIncludesClearanceAtPlacementTime()
        {
            ReportHostile(new FormationPoint(1000f, 0f));
            Assert.That(_recovery.IsPlacementSafe(Vector3.right * 500f, 0f), Is.False);
            Assert.That(_recovery.IsPlacementSafe(Vector3.right * 350f, 0f), Is.True);
            Assert.That(_recovery.IsPlacementSafe(Vector3.right * 350f, 100f), Is.False);
        }

        [Test]
        public void RememberedHostileStructureExcludesEmptySectorAfterItLeavesVision()
        {
            _intel.GetPatrol(_owner.Team).Register("structure:123", new FormationPoint(1000f, 0f), "hostileStructure", 2f);
            Assert.That(_recovery.IsPlacementSafe(Vector3.right * 500f, 0f), Is.False);
            Assert.That(_recovery.GetNearestHostileDistance(Vector3.right * 500f), Is.EqualTo(500f));
        }

        private void ReportHostile(FormationPoint position) =>
            _intel.Get(_owner.Team).Report(new HostileSighting(123, UnitTypeId.Ship(ShipType.Venator),
                TestPlayers.Enemy, position, 100f, 0f, 1f, Time.time));

        private void Update(bool canCapture = true) =>
            _recovery.Update(Vector3.zero, 0.5f, false, false, Time.time, (position, radius) => true, canCapture);

        private ReinforcementZonePresenter AddZone(PlayerId owner, Vector3 center)
        {
            var zone = new ReinforcementZonePresenter(new ZoneView(center), TestPlayers.CreateLocalPlayer(_roster),
                new ReinforcementZoneModel(_roster, owner, 10f, 1f, true));
            _zones.Items.Add(zone);
            return zone;
        }

        private sealed class ZoneSource : IReinforcementZoneSource
        {
            public List<ReinforcementZonePresenter> Items { get; } = new List<ReinforcementZonePresenter>();
            public IReadOnlyList<ReinforcementZonePresenter> Zones => Items;
        }

        private sealed class Map : IMapModelObserver
        {
            public Vector2Range SizeRange { get; } = new Vector2Range();
            public Map()
            {
                var type = typeof(Vector2Range).BaseType;
                type.GetField("<Min>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(SizeRange, Vector2.one * -3000f);
                type.GetField("<Max>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(SizeRange, Vector2.one * 3000f);
            }
            public Vector3 GetStationPosition(PlayerId owner) => new Vector3(-2000f, 0f, 0f);
        }

        private sealed class ZoneView : IReinforcementZoneView
        {
            public Vector3 Center { get; }
            public float Radius => 10f;
            public PlayerId StartingOwner => PlayerId.None;
            public bool IsCapturable => true;
            public float CaptureDuration => 10f;
            public ZoneView(Vector3 center) { Center = center; }
            public void SetVisibility(bool isVisible, bool showCaptureUi) { }
            public void Render(OwnerRelation owner, OwnerRelation capturer, float captureProgress, bool isContested,
                OwnerRelation contestingFirst = OwnerRelation.Neutral, OwnerRelation contestingSecond = OwnerRelation.Neutral) { }
        }
    }
}
