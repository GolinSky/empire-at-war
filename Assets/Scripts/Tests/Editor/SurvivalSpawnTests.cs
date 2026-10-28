using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Battle;
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
        public void EdgeSpawns_RetryBlockedFootprintsAndRemainSeparatedInsideBounds()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            var map = new MapStub(settings.GetSize(MapSize.Small).Bounds);
            var clearance = new ClearanceStub(); var points = new EdgeSpawnPointProvider(map, clearance);
            points.BeginWave();
            Assert.That(points.TryGet(new PlayerId(1), UnitTypeId.Ship(ShipType.Venator), 0, out var first), Is.True);
            Assert.That(clearance.Attempts, Is.GreaterThanOrEqualTo(3));
            Assert.That(points.TryGet(new PlayerId(1), UnitTypeId.Ship(ShipType.Venator), 0, out var second), Is.True);
            Assert.That(Vector3.Distance(first, second), Is.GreaterThanOrEqualTo(140));
            foreach (var position in new[] { first, second })
            {
                Assert.That(position.x, Is.InRange(map.SizeRange.Min.x + 90, map.SizeRange.Max.x - 90));
                Assert.That(position.z, Is.InRange(map.SizeRange.Min.y + 90, map.SizeRange.Max.y - 90));
            }
        }
        [Test]
        public void EdgeSpawns_ReuseEdgeOnceWaveReservationsFillIt()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            var map = new MapStub(settings.GetSize(MapSize.Small).Bounds);
            // Hulls this wide leave room for only a few reserved points per edge.
            float radius = (map.SizeRange.Max.x - map.SizeRange.Min.x) / 5;
            var points = new EdgeSpawnPointProvider(map, new ClearanceStub { Attempts = 2, Radius = radius });
            points.BeginWave();
            for (int spawn = 0; spawn < 50; spawn++)
                Assert.That(points.TryGet(new PlayerId(1), UnitTypeId.Ship(ShipType.Venator), spawn % 3, out _), Is.True);
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
            public float Radius = 50;
            public float GetPlanarRadius(ShipType type) => Radius;
            public bool IsClear(PlayerId owner, ShipType type, Vector3 position) => ++Attempts > 2;
            public void ReserveLanding(object ship, PlayerId owner, ShipType type, Vector3 position) { }
            public void ReleaseLanding(object ship) { }
        }
    }
}
