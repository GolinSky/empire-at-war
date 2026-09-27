using System.Linq;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AuditSharedOperationsTests
    {
        [Test]
        public void ShipCounts_UseAreaAndFactionAndReadCurrentMembership()
        {
            ShipService ships = new ShipService();
            FakeShip player = new FakeShip(PlayerType.Player, 5f);
            ships.Add(player);
            ships.Add(new FakeShip(PlayerType.Opponent, 3f));
            ships.Add(new FakeShip(PlayerType.Opponent, 8f));
            ships.Add(new FakeShip(PlayerType.None, 2f));

            ships.CountShips(position => position.x <= 5f, out int players, out int opponents);
            Assert.That(players, Is.EqualTo(1));
            Assert.That(opponents, Is.EqualTo(1));

            ships.Remove(player);
            ships.CountShips(position => position.x <= 5f, out players, out opponents);
            Assert.That(players, Is.Zero);
            Assert.That(opponents, Is.EqualTo(1));
            ships.CountShips(position => false, out players, out opponents);
            Assert.That(players, Is.Zero);
            Assert.That(opponents, Is.Zero);
        }

        [Test]
        public void MarkerCleanup_RemovesOnlyOwnedMarkersAndCanBeRepeated()
        {
            MiniMapData data = ScriptableObject.CreateInstance<MiniMapData>();
            try
            {
                MiniMapMarkerCollection<string> sites = new MiniMapMarkerCollection<string>(data);
                MiniMapMarkerCollection<string> zones = new MiniMapMarkerCollection<string>(data);
                MiniMapMarker site = new MiniMapMarker(MarkType.CaptureSite, PlayerType.Player);
                MiniMapMarker zone = new MiniMapMarker(MarkType.ReinforcementZone, PlayerType.Opponent);
                int removed = 0;
                data.OnMarkerRemoved += marker => removed++;
                sites.Add("site", site);
                zones.Add("zone", zone);
                Assert.That(data.Markers, Is.EquivalentTo(new[] { site, zone }));
                Assert.That(sites.Pairs.Single().Value, Is.SameAs(site));

                sites.Clear();
                sites.Clear();
                Assert.That(data.Markers, Is.EqualTo(new[] { zone }));
                Assert.That(sites.Pairs, Is.Empty);
                Assert.That(removed, Is.EqualTo(1));

                sites.Add("site", site);
                zones.Clear();
                Assert.That(data.Markers, Is.EqualTo(new[] { site }));
                sites.Clear();
                Assert.That(data.Markers, Is.Empty);
                Assert.That(removed, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        private sealed class FakeShip : IShipEntity
        {
            public FakeShip(PlayerType playerType, float x)
            {
                PlayerType = playerType;
                WorldPosition = new Vector3(x, 0f, 0f);
            }

            public IShipModelObserver ModelObserver => null;
            public PlayerType PlayerType { get; }
            public Vector3 WorldPosition { get; }
            public float NavigationRadius => 1f;
            public float NavigationSpeed => 1f;
            public long EntityId => 0;
            public UnitOrderType CurrentOrder => UnitOrderType.None;
        }
    }
}
