using System;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SpawnArea;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.StationFacing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class ShipSpawnClearanceGridTests
    {
        private const ShipType SHIP_TYPE = ShipType.ISDI;
        private GameObject _hullObject;
        private GameObject _obstacleObject;
        private ShipSpawnClearance _clearance;
        private float _height;
        private readonly PlayerId _owner = new PlayerId(0);

        [SetUp]
        public void SetUp()
        {
            _hullObject = new GameObject("Grid hull");
            BoxCollider hull = _hullObject.AddComponent<BoxCollider>();
            hull.center = new Vector3(3f, 0f, -2f);
            hull.size = new Vector3(4f, 2f, 8f);
            _hullObject.SetActive(false);
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>(
                "Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            var assets = new TestAssets(hull);
            _height = assets.Load<ShipData>(ships.GetShipDataPath(SHIP_TYPE)).Height;
            _clearance = new ShipSpawnClearance(assets, new FixedFacing(), ships);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_hullObject);
            if (_obstacleObject != null) Object.DestroyImmediate(_obstacleObject);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BlockObstructedCells_MatchesExactClearanceForOffsetRotatedHull(bool reservation)
        {
            if (reservation)
                _clearance.ReserveLanding(this, _owner, SHIP_TYPE, new Vector3(9f, 0f, 0f));
            else
            {
                _obstacleObject = new GameObject("Grid obstacle");
                BoxCollider obstacle = _obstacleObject.AddComponent<BoxCollider>();
                obstacle.size = new Vector3(4f, 2f, 6f);
                // Outside the map, but the hull at the edge can still intersect it.
                _obstacleObject.transform.position = new Vector3(12f, _height, 0f);
                Physics.SyncTransforms();
            }

            var grid = new SpawnAreaGridModel(20, -10f, -10f, 20f, 20f);
            grid.RevealAll();
            int queries = 0;
            _clearance.BlockObstructedCells(_owner, SHIP_TYPE, grid,
                new Bounds(Vector3.zero, new Vector3(20f, 0f, 20f)), (x, z) =>
                {
                    queries++;
                    return !_clearance.IsClear(_owner, SHIP_TYPE, new Vector3(x, 0f, z));
                });

            int blocked = 0;
            for (int row = 0; row < 20; row++)
                for (int column = 0; column < 20; column++)
                {
                    bool clear = _clearance.IsClear(_owner, SHIP_TYPE,
                        new Vector3(-9.5f + column, 0f, -9.5f + row));
                    Assert.That(grid.Get(column, row),
                        Is.EqualTo(clear ? SpawnAreaGridModel.OPEN : SpawnAreaGridModel.BLOCKED));
                    if (!clear) blocked++;
                }
            Assert.That(blocked, Is.GreaterThan(0));
            Assert.That(queries, Is.LessThan(400));
        }

        [Test]
        public void BlockObstructedCells_DoesNotQueryColliderAboveHull()
        {
            _obstacleObject = new GameObject("High obstacle");
            BoxCollider obstacle = _obstacleObject.AddComponent<BoxCollider>();
            obstacle.size = Vector3.one * 2f;
            _obstacleObject.transform.position = new Vector3(0f, _height + 20f, 0f);
            Physics.SyncTransforms();
            var grid = new SpawnAreaGridModel(20, -10f, -10f, 20f, 20f);
            grid.RevealAll();
            _clearance.BlockObstructedCells(_owner, SHIP_TYPE, grid,
                new Bounds(Vector3.zero, new Vector3(20f, 0f, 20f)),
                (x, z) => throw new InvalidOperationException("No candidate expected."));
        }

        private sealed class TestAssets : IAssetService
        {
            private readonly BoxCollider _hull;
            public TestAssets(BoxCollider hull) => _hull = hull;
            public TSource Load<TSource>(string key) where TSource : Object =>
                AssetDatabase.LoadAssetAtPath<TSource>(AssetDatabase.GUIDToAssetPath(key));
            public TComponent LoadComponent<TComponent>(string key) where TComponent : Component =>
                (TComponent)(Component)_hull;
            public GameObject LoadPrefab(string key) => throw new NotSupportedException();
        }

        private sealed class FixedFacing : IStationFacingService
        {
            public Quaternion GetRotation(PlayerId owner) => Quaternion.Euler(0f, 90f, 0f);
        }
    }
}
