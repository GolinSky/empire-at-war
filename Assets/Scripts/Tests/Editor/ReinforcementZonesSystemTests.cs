using EmpireAtWar.Entities.Capture;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using System.Reflection;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Ship;
using EmpireAtWar.Utils;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Services.Ships;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ReinforcementZonesSystemTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void RelaySave_JsonRoundTripRestoresCaptureAndRelayServices()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            VisionService vision = new VisionService(roster);
            SpawnBlockerService blockers = new SpawnBlockerService(roster);
            try
            {
                ReinforcementZoneView zone = CreateZone(root.transform, PlayerId.None, true, Vector3.zero);
                ReinforcementZonesSystem system = CreateSystem(root, data, blockers, vision, zone);
                CaptureStrengthBuilder strength = new CaptureStrengthBuilder(roster);
                strength.Add(TestPlayers.Enemy, 1f);
                system.Zones[0].Tick(zone.CaptureDuration / data.CaptureSpeedPerNetShip, strength.Build());
                strength.Clear();
                strength.Add(TestPlayers.Human, 2f);
                strength.Add(TestPlayers.Enemy, 1f);
                system.Zones[0].Tick(zone.CaptureDuration * 0.25f / data.CaptureSpeedPerNetShip, strength.Build());
                strength.Add(TestPlayers.Enemy, 1f);
                system.Zones[0].Tick(1f, strength.Build());
                ReinforcementZonesSaveSource source = new ReinforcementZonesSaveSource(system);
                BattleSave save = new BattleSave();
                source.Capture(save);
                BattleSave restored = BattleSave.FromJson(save.ToJson());
                system.UpdateState(CreateMap(zone));
                Assert.That(system.Zones[0].Owner, Is.EqualTo(PlayerId.None));
                int changes = 0;
                system.OwnershipChanged += () =>
                {
                    changes++;
                    Assert.That(vision.IsVisible(TestPlayers.Enemy, zone.Center), Is.True);
                    Assert.That(blockers.IsBlocked(TestPlayers.Enemy, zone.Center), Is.False);
                };

                source.RestoreBeforeUnits(restored);

                Assert.That(system.Zones[0].Owner, Is.EqualTo(TestPlayers.Enemy));
                Assert.That(system.Zones[0].CapturingPlayer, Is.EqualTo(TestPlayers.Human));
                Assert.That(system.Zones[0].CaptureProgress, Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(system.Zones[0].IsContested, Is.True);
                Assert.That(system.GetOwnedCapturableZoneCount(TestPlayers.Enemy), Is.EqualTo(1));
                Assert.That(vision.IsVisible(TestPlayers.SecondEnemy, zone.Center), Is.True);
                Assert.That(vision.IsVisible(TestPlayers.Human, zone.Center), Is.False);
                Assert.That(blockers.IsBlocked(TestPlayers.Human, zone.Center), Is.True);
                Assert.That(system.Zones[0].IsRevealed, Is.False);
                Assert.That(changes, Is.EqualTo(1));

                strength.Clear();
                strength.Add(TestPlayers.Human, 1f);
                system.Zones[0].Tick(zone.CaptureDuration * 0.75f / data.CaptureSpeedPerNetShip, strength.Build());
                Assert.That(system.Zones[0].Owner, Is.EqualTo(TestPlayers.Human),
                    "Capture must continue from saved progress.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void RelaySave_RestoringNeutralRemovesPreviousVisionAndBlocksEveryTeam()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            VisionService vision = new VisionService(roster);
            SpawnBlockerService blockers = new SpawnBlockerService(roster);
            try
            {
                ReinforcementZoneView zone = CreateZone(root.transform, TestPlayers.Human, true, Vector3.zero);
                ReinforcementZonesSystem system = CreateSystem(root, data, blockers, vision, zone);
                Assert.That(vision.IsVisible(TestPlayers.Human, zone.Center), Is.True);

                system.RestoreZones(new[]
                {
                    ReinforcementZoneSave.Create(PlayerId.None, PlayerId.None, 0f, false)
                });

                Assert.That(system.Zones[0].Owner, Is.EqualTo(PlayerId.None));
                Assert.That(vision.Sources, Is.Empty);
                Assert.That(system.Zones[0].IsRevealed, Is.False);
                Assert.That(blockers.IsBlocked(TestPlayers.Human, zone.Center), Is.True);
                Assert.That(blockers.IsBlocked(TestPlayers.Enemy, zone.Center), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void RelaySave_RejectsMismatchedZoneCountBeforeChangingState()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZonesSystem system = CreateSystem(root, data,
                    CreateZone(root.transform, TestPlayers.Human, true, Vector3.zero));
                int changes = 0;
                system.OwnershipChanged += () => changes++;

                Assert.Throws<System.InvalidOperationException>(() =>
                    system.RestoreZones(System.Array.Empty<ReinforcementZoneSave>()));

                Assert.That(system.Zones[0].Owner, Is.EqualTo(TestPlayers.Human));
                Assert.That(changes, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void StructureClearance_RejectsFootprintOverlappingZone()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView zone = CreateZone(root.transform, TestPlayers.Enemy,
                    true, Vector3.zero);
                ReinforcementZonesSystem system = CreateSystem(root, data, zone);

                Assert.That(system.IsPositionInAnyZone(new Vector3(50f, 0f, 0f)), Is.False);
                Assert.That(system.IsPositionInAnyZone(new Vector3(50f, 0f, 0f), 16f), Is.True);
                Assert.That(system.IsPositionInAnyZone(new Vector3(64f, 0f, 0f), 16f), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void StructureFallback_UsesOnlyOwnedCapturableZonesAndClearsPreviousBounds()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView captured = CreateZone(
                    root.transform, TestPlayers.Enemy, true, new Vector3(55f, 0f, -55f));
                ReinforcementZonesSystem system = CreateSystem(root, data,
                    CreateZone(root.transform, TestPlayers.Enemy, false, Vector3.zero),
                    captured,
                    CreateZone(root.transform, TestPlayers.Human, true, Vector3.left * 80f),
                    CreateZone(root.transform, PlayerId.None, true, Vector3.right * 80f));
                List<Bounds> bounds = new List<Bounds> { new Bounds(Vector3.one, Vector3.one) };

                system.CopyOwnedCapturableZoneBounds(TestPlayers.Enemy, bounds);

                Assert.That(bounds, Has.Count.EqualTo(1));
                Assert.That(bounds[0].center, Is.EqualTo(captured.Center));
                Assert.That(bounds[0].extents.x, Is.EqualTo(captured.Radius));
                Assert.That(bounds[0].extents.z, Is.EqualTo(captured.Radius));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Relay_BlocksSpawningForHostilesOfItsOwnerTeamOnly()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            SpawnBlockerService blockers = new SpawnBlockerService(TestPlayers.CreateTeamGame());
            try
            {
                ReinforcementZoneView allied = CreateZone(
                    root.transform, TestPlayers.Ally, false, Vector3.zero);
                CreateSystem(root, data, blockers, allied);

                Assert.That(blockers.IsBlocked(TestPlayers.Human, allied.Center), Is.False);
                Assert.That(blockers.IsBlocked(TestPlayers.Enemy, allied.Center), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void NeutralRelay_BlocksEveryTeam()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            SpawnBlockerService blockers = new SpawnBlockerService(TestPlayers.CreateTeamGame());
            try
            {
                ReinforcementZoneView neutral = CreateZone(
                    root.transform, PlayerId.None, true, Vector3.zero);
                CreateSystem(root, data, blockers, neutral);

                Assert.That(blockers.IsBlocked(TestPlayers.Human, neutral.Center), Is.True);
                Assert.That(blockers.IsBlocked(TestPlayers.Enemy, neutral.Center), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OwnedRelay_RevealsItsSpawnRangeForItsTeam(bool isCapturable)
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            VisionService vision = new VisionService(TestPlayers.CreateTeamGame());
            try
            {
                SetField(data, "<RelaySpawnBlockRadius>k__BackingField", 1234f);
                ReinforcementZoneView zone = CreateZone(root.transform, TestPlayers.Ally,
                    isCapturable, new Vector3(100f, 200f, -300f));
                CreateSystem(root, data, new SpawnBlockerService(TestPlayers.CreateTeamGame()), vision, zone);
                Vector3 edge = zone.Center + Vector3.right * data.RelaySpawnBlockRadius;

                Assert.That(vision.IsVisible(TestPlayers.Ally, edge), Is.True);
                Assert.That(vision.IsVisible(TestPlayers.Human, edge + Vector3.up * 500f), Is.True);
                Assert.That(vision.IsVisible(TestPlayers.Enemy, zone.Center), Is.False);
                Assert.That(vision.IsVisible(TestPlayers.Human, edge + Vector3.right), Is.False);
                Assert.That(vision.Sources, Has.Count.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Relay_CaptureTransfersVisionAndKeepsItWithoutUnits(bool initiallyOwned)
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            VisionService vision = new VisionService(roster);
            SpawnBlockerService blockers = new SpawnBlockerService(roster);
            try
            {
                ReinforcementZoneView zone = CreateZone(root.transform,
                    initiallyOwned ? TestPlayers.Human : PlayerId.None, true, Vector3.zero);
                ReinforcementZonesSystem system = CreateSystem(root, data, blockers, vision, zone);
                CaptureSquadrons squadrons = new CaptureSquadrons { Owner = TestPlayers.Enemy };
                CaptureStrengthSampler sampler = new CaptureStrengthSampler(new ShipService(), squadrons, roster, data.SquadronCaptureWeight);
                foreach (var presenter in system.Zones) sampler.Add(presenter.Contains);
                SetField(system, "_strengthSampler", sampler);
                Vector3 edge = Vector3.right * data.RelaySpawnBlockRadius;
                Assert.That(vision.IsVisible(TestPlayers.Human, edge), Is.EqualTo(initiallyOwned));
                Assert.That(vision.IsVisible(TestPlayers.Enemy, edge), Is.False);
                if (!initiallyOwned) Assert.That(vision.Sources, Is.Empty);
                int changes = 0;
                system.OwnershipChanged += () =>
                {
                    changes++;
                    Assert.That(vision.IsVisible(TestPlayers.Enemy, edge), Is.True);
                    Assert.That(vision.IsVisible(TestPlayers.Human, edge), Is.False);
                    Assert.That(blockers.IsBlocked(TestPlayers.Enemy, edge), Is.False);
                };

                MethodInfo tick = typeof(ReinforcementZonesSystem).GetMethod("TickZones", PRIVATE_INSTANCE);
                tick.Invoke(system, new object[] { zone.CaptureDuration / data.SquadronCaptureWeight });
                Assert.That(system.Zones[0].Owner, Is.EqualTo(TestPlayers.Enemy));
                squadrons.Owner = PlayerId.None;
                tick.Invoke(system, new object[] { zone.CaptureDuration });

                Assert.That(changes, Is.EqualTo(1));
                Assert.That(vision.IsVisible(TestPlayers.Enemy, edge), Is.True);
                Assert.That(vision.IsVisible(TestPlayers.SecondEnemy, edge), Is.True);
                Assert.That(vision.IsVisible(TestPlayers.Human, edge), Is.False);
                Assert.That(blockers.IsBlocked(TestPlayers.Human, edge), Is.True);
                Assert.That(vision.Sources, Has.Count.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void HomeVisionAnchors_AreReusedWhenTheMapIsReplaced()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            VisionService vision = new VisionService(TestPlayers.CreateTeamGame());
            try
            {
                ReinforcementZonesSystem system = CreateSystem(root, data,
                    new SpawnBlockerService(TestPlayers.CreateTeamGame()), vision);
                ZoneSpot[] homes =
                {
                    new ZoneSpot(new Vector3(-100f, 0f, 0f), TestPlayers.Human, false),
                    new ZoneSpot(new Vector3(100f, 0f, 0f), TestPlayers.Enemy, false)
                };

                system.UpdateState(CreateMap(homes));
                system.UpdateState(CreateMap(homes));
                system.UpdateState(CreateMap(new[] { homes[1] }));

                Assert.That(root.transform.childCount, Is.EqualTo(2));
                Assert.That(vision.Sources.Count, Is.EqualTo(1));
                Assert.That(vision.IsVisible(TestPlayers.Enemy, homes[1].Center), Is.True);
                Assert.That(vision.IsVisible(TestPlayers.Human, homes[0].Center), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RelayVisionAndBlockers_AreRemovedOnDisposalOrMapReplacement(bool dispose)
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            VisionService vision = new VisionService(TestPlayers.CreateTeamGame());
            try
            {
                ReinforcementZoneView zone = CreateZone(root.transform, TestPlayers.Human, true, Vector3.zero);
                SpawnBlockerService blockers = new SpawnBlockerService(TestPlayers.CreateTeamGame());
                ReinforcementZonesSystem system = CreateSystem(root, data, blockers, vision, zone);
                BattleMap emptyMap = CreateMap();
                SetField(system, "_battleMap", new ReplayNotifier<BattleMap>(emptyMap));
                Assert.That(vision.IsVisible(TestPlayers.Human, zone.Center), Is.True);

                if (dispose) system.LateDispose();
                else system.UpdateState(emptyMap);
                Object.DestroyImmediate(zone.gameObject);

                Assert.That(vision.Sources, Is.Empty);
                Assert.That(vision.IsVisible(TestPlayers.Human, Vector3.zero), Is.False);
                Assert.That(blockers.Blockers, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Relay_StrengthRefreshIsThrottledWhileCaptureProgressAdvancesEveryTick()
        {
            GameObject root = new GameObject(nameof(ReinforcementZonesSystemTests));
            ReinforcementZoneData data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            try
            {
                ReinforcementZoneView zone = CreateZone(root.transform, PlayerId.None, true, Vector3.zero);
                ReinforcementZonesSystem system = CreateSystem(root, data, zone);
                CaptureSquadrons squadrons = new CaptureSquadrons { Owner = TestPlayers.Enemy };
                CaptureStrengthSampler sampler = new CaptureStrengthSampler(new ShipService(), squadrons, TestPlayers.CreateTeamGame(), data.SquadronCaptureWeight);
                foreach (var presenter in system.Zones) sampler.Add(presenter.Contains);
                SetField(system, "_strengthSampler", sampler);
                MethodInfo tick = typeof(ReinforcementZonesSystem).GetMethod("TickZones", PRIVATE_INSTANCE);
                tick.Invoke(system, new object[] { 0.1f });
                float progress = system.Zones[0].CaptureProgress;
                tick.Invoke(system, new object[] { 0.1f });
                Assert.That(squadrons.ScanCount, Is.EqualTo(1));
                Assert.That(system.Zones[0].CaptureProgress, Is.GreaterThan(progress));
                tick.Invoke(system, new object[] { 0.16f });
                Assert.That(squadrons.ScanCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        private static ReinforcementZonesSystem CreateSystem(GameObject root,
            ReinforcementZoneData data, params ReinforcementZoneView[] zones) =>
            CreateSystem(root, data, new SpawnBlockerService(TestPlayers.CreateTeamGame()), zones);

        private static ReinforcementZonesSystem CreateSystem(GameObject root,
            ReinforcementZoneData data, SpawnBlockerService blockers, params ReinforcementZoneView[] zones)
            => CreateSystem(root, data, blockers, new VisionService(TestPlayers.CreateTeamGame()), zones);

        private static ReinforcementZonesSystem CreateSystem(GameObject root,
            ReinforcementZoneData data, SpawnBlockerService blockers, VisionService vision,
            params ReinforcementZoneView[] zones)
        {
            ReinforcementZonesSystem system = root.AddComponent<ReinforcementZonesSystem>();
            SetField(system, "_data", data);
            SetField(system, "_mapModel", new FakeMapModel(
                new Vector3(-180f, 0f, 170f), new Vector3(160f, 0f, -170f)));
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            SetField(system, "_playerRoster", roster);
            SetField(system, "_localPlayer", TestPlayers.CreateLocalPlayer(roster));
            SetField(system, "_spawnBlockerService", blockers);
            SetField(system, "_visionService", vision);
            SetField(system, "_strengthSampler", new CaptureStrengthSampler(new ShipService(), new CaptureSquadrons(), roster, data.SquadronCaptureWeight));
            system.UpdateState(CreateMap(zones));
            return system;
        }

        private static BattleMap CreateMap(params ReinforcementZoneView[] zones) =>
            CreateMap(System.Array.Empty<ZoneSpot>(), zones);

        private static BattleMap CreateMap(ZoneSpot[] spots, params ReinforcementZoneView[] zones) => new BattleMap(
                layout: new MapLayout(
                    stationPositions: new Dictionary<PlayerId, Vector3>(),
                    zones: spots,
                    sites: System.Array.Empty<SiteSpot>(),
                    lanes: System.Array.Empty<MapLane>(),
                    fields: System.Array.Empty<AsteroidField>(),
                    sizeRange: new Vector2Range(),
                    planetPosition: Vector3.zero),
                zoneViews: zones,
                siteViews: System.Array.Empty<CaptureSiteView>(),
                obstacles: System.Array.Empty<MapObstacle>(),
                stationObstacles: System.Array.Empty<StationObstacle>());

        private static ReinforcementZoneView CreateZone(
            Transform parent,
            PlayerId owner,
            bool isCapturable,
            Vector3 center)
        {
            GameObject gameObject = new GameObject($"{owner}Zone");
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = center;
            ReinforcementZoneView view = gameObject.AddComponent<ReinforcementZoneView>();
            CaptureRingView ring = gameObject.AddComponent<CaptureRingView>();
            SetField(ring, "ringRenderer", gameObject.AddComponent<MeshRenderer>());
            SetField(view, "captureRing", ring);
            GameObject captureUi = new GameObject("CaptureUi", typeof(RectTransform));
            captureUi.transform.SetParent(gameObject.transform);
            SetField(view, "captureCanvas", captureUi.AddComponent<Canvas>());
            SetField(view, "_startingOwner", owner);
            SetField(view, "isCapturable", isCapturable);
            return view;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private sealed class CaptureSquadrons : ISquadronRegistry
        {
            public string Id => nameof(CaptureSquadrons);
            public PlayerId Owner { get; set; } = PlayerId.None;
            public int ScanCount { get; private set; }

            public void AddSquadronStrength(System.Func<Vector3, bool> contains, float weight,
                CaptureStrengthBuilder tally)
            {
                ScanCount++;
                if (Owner != PlayerId.None && contains(Vector3.zero)) tally.Add(Owner, weight);
            }

            public bool HasSquadronInside(System.Func<Vector3, bool> contains,
                System.Predicate<PlayerId> isOwnerIncluded) =>
                Owner != PlayerId.None && contains(Vector3.zero) && isOwnerIncluded(Owner);
        }

        private sealed class FakeMapModel : IMapModelObserver
        {
            private readonly Vector2Range _sizeRange;

            private readonly Vector3 _republicPosition;
            private readonly Vector3 _separatistPosition;

            public Vector2Range SizeRange => _sizeRange;

            public FakeMapModel(Vector3 republicPosition, Vector3 separatistPosition)
            {
                _republicPosition = republicPosition;
                _separatistPosition = separatistPosition;
                _sizeRange = new Vector2Range();
                SetRangeValue("<Min>k__BackingField", new Vector2(-250f, -250f));
                SetRangeValue("<Max>k__BackingField", new Vector2(250f, 250f));
            }

            public Vector3 GetStationPosition(PlayerId owner)
            {
                return owner == TestPlayers.Human ? _republicPosition : _separatistPosition;
            }

            private void SetRangeValue(string fieldName, Vector2 value)
            {
                FieldInfo field = _sizeRange.GetType().BaseType?.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                field.SetValue(_sizeRange, value);
            }
        }
    }
}
