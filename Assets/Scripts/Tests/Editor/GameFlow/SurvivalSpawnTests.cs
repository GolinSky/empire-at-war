using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ShipSpawning;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class SurvivalSpawnTests
    {
        [Test]
        public void EdgeSpawns_RetryBlockedShipFootprintsInsideBounds()
        {
            var map = CreateMap();
            var clearance = new ClearanceStub();
            var points = new EdgeSpawnPoints(map, clearance, new TestHazards());
            points.BeginBatch();
            Assert.That(points.TryGet(new PlayerId(1), UnitTypeId.Ship(ShipType.Venator), 0, out var position), Is.True);
            Assert.That(clearance.Attempts, Is.GreaterThanOrEqualTo(3));
            Assert.That(position.x, Is.InRange(map.SizeRange.Min.x + 90, map.SizeRange.Max.x - 90));
            Assert.That(position.z, Is.InRange(map.SizeRange.Min.y + 90, map.SizeRange.Max.y - 90));
        }

        [Test]
        public void EdgeSpawns_KeepSquadronsSeparated()
        {
            var points = new EdgeSpawnPoints(CreateMap(), new ClearanceStub { Attempts = 2 }, new TestHazards());
            points.BeginBatch();
            Assert.That(points.TryGet(new PlayerId(1), UnitTypeId.Squadron(SquadronType.Delta7), 0, out var first), Is.True);
            Assert.That(points.TryGet(new PlayerId(1), UnitTypeId.Squadron(SquadronType.Delta7), 0, out var second), Is.True);
            Assert.That(Vector3.Distance(first, second), Is.GreaterThanOrEqualTo(120));
        }

        [Test]
        public void EdgeSpawns_ReuseEdgeOnceBatchReservationsFillIt()
        {
            var points = new EdgeSpawnPoints(CreateMap(), new ClearanceStub { Attempts = 2 }, new TestHazards());
            points.BeginBatch();
            for (int spawn = 0; spawn < 200; spawn++)
                Assert.That(points.TryGet(new PlayerId(1), UnitTypeId.Squadron(SquadronType.Delta7), spawn % 3, out _), Is.True);
        }

        [Test]
        public void EdgeSpawns_SkipReinforcementBlockingHazards()
        {
            var points = new EdgeSpawnPoints(CreateMap(), new ClearanceStub { Attempts = 2 }, new TestHazards { Blocks = true });
            points.BeginBatch();
            Assert.That(points.TryGet(new PlayerId(1), UnitTypeId.Ship(ShipType.Venator), 0, out _), Is.False);
        }

        private static MapStub CreateMap()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            return new MapStub(settings.GetSize(MapSize.Small).Bounds);
        }

        private sealed class MapStub : IMapModelObserver
        {
            public Vector2Range SizeRange { get; }
            public MapStub(Vector2Range range) => SizeRange = range;
            public Vector3 GetStationPosition(PlayerId owner) => Vector3.zero;
        }
        private sealed class ClearanceStub : IShipSpawnClearance
        {
            public int Attempts;
            public float GetPlanarRadius(ShipType type) => 50;
            public bool IsClear(PlayerId owner, ShipType type, Vector3 position) => ++Attempts > 2;
            public void ReserveLanding(object ship, PlayerId owner, ShipType type, Vector3 position) { }
            public void ReleaseLanding(object ship) { }
        }
    }
}
