using System.Linq;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity.Orders;
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
        public void ShipStrength_UsesAreaAndOwnerAndReadsCurrentMembership()
        {
            ShipService ships = new ShipService();
            FakeShip player = new FakeShip(TestPlayers.Human, 5f);
            ships.Add(player);
            ships.Add(new FakeShip(TestPlayers.Enemy, 3f));
            ships.Add(new FakeShip(TestPlayers.Enemy, 8f));
            ships.Add(new FakeShip(PlayerId.None, 2f));
            CaptureTallyBuilder tally = new CaptureTallyBuilder(TestPlayers.CreateDuel());

            ships.AddShipStrength(position => position.x <= 5f, tally);
            CaptureTally bothSides = tally.Build();
            Assert.That(bothSides.PresentTeamCount, Is.EqualTo(2));
            Assert.That(bothSides.IsContested, Is.True);

            ships.Remove(player);
            tally.Clear();
            ships.AddShipStrength(position => position.x <= 5f, tally);
            CaptureTally enemyOnly = tally.Build();
            Assert.That(enemyOnly.LeadingPlayer, Is.EqualTo(TestPlayers.Enemy));
            Assert.That(enemyOnly.Advantage, Is.EqualTo(1f));

            tally.Clear();
            ships.AddShipStrength(position => false, tally);
            Assert.That(tally.Build().HasUnits, Is.False);
        }

        [Test]
        public void MarkerCleanup_RemovesOnlyOwnedMarkersAndCanBeRepeated()
        {
            MiniMapData data = ScriptableObject.CreateInstance<MiniMapData>();
            try
            {
                MiniMapMarkerCollection<string> sites = new MiniMapMarkerCollection<string>(data);
                MiniMapMarkerCollection<string> zones = new MiniMapMarkerCollection<string>(data);
                MiniMapMarker site = new MiniMapMarker(MarkType.CaptureSite, OwnerRelation.Own);
                MiniMapMarker zone = new MiniMapMarker(MarkType.ReinforcementZone, OwnerRelation.Enemy);
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
            public FakeShip(PlayerId owner, float x)
            {
                Owner = owner;
                WorldPosition = new Vector3(x, 0f, 0f);
            }

            public IShipModelObserver ModelObserver => null;
            public PlayerId Owner { get; }
            public Vector3 WorldPosition { get; }
            public float NavigationRadius => 1f;
            public float NavigationSpeed => 1f;
            public long EntityId => 0;
            public UnitOrderType CurrentOrder => UnitOrderType.None;
        }
    }
}
