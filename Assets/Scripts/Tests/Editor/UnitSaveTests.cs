using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitSaveTests
    {
        [Test]
        public void Order_WaypointMove_KeepsOnlyUnreachedWaypoints()
        {
            UnitOrderModel order = new UnitOrderModel();
            FormationPoint[] points = { new FormationPoint(1f, 2f), new FormationPoint(3f, 4f), new FormationPoint(5f, 6f) };
            order.Replace(UnitOrderType.WaypointMove, points[0], waypoints: points);
            order.AdvanceWaypoint(out _);

            UnitOrderSave save = RoundTrip(UnitOrderSave.From(order));

            Assert.That(save.Type, Is.EqualTo(UnitOrderType.WaypointMove));
            Assert.That(JsonUtility.ToJson(save), Does.Contain("\"x\":3.0").And.Not.Contain("\"x\":1.0"));
        }

        [Test]
        public void Order_AbilityApproach_IsNotSaved()
        {
            UnitOrderModel order = new UnitOrderModel();
            order.Replace(UnitOrderType.UseAbility);

            Assert.That(UnitOrderSave.From(order).Type, Is.EqualTo(UnitOrderType.None));
        }

        [Test]
        public void Unit_JsonRoundTrip_KeepsIdentityPoseHealthAndHangar()
        {
            UnitSave unit = UnitSave.Create(-8_123_456_789_012L, new PlayerId(2), UnitTypeId.Ship(ShipType.Venator),
                new Vector3(10f, -20f, 30f), Quaternion.Euler(0f, 90f, 0f),
                UnitHealthSave.ForShip(0.5f, 0.25f, new[] { 1f, 0f }), UnitOrderSave.From(new UnitOrderModel()))
                .WithHangar(HangarSave.Create(new[] { 3, 0 }, 2.5f, new[] { LaunchedSquadronSave.Create(77L, 1) }));

            UnitSave loaded = JsonUtility.FromJson<UnitSave>(JsonUtility.ToJson(unit));

            Assert.That(loaded.Id, Is.EqualTo(-8_123_456_789_012L), "Ids use the full long range.");
            Assert.That(loaded.Owner, Is.EqualTo(new PlayerId(2)));
            Assert.That(loaded.UnitType, Is.EqualTo(UnitTypeId.Ship(ShipType.Venator)));
            Assert.That(loaded.Position, Is.EqualTo(new Vector3(10f, -20f, 30f)));
            Assert.That(loaded.Health.HardPoints, Is.EqualTo(new[] { 1f, 0f }));
            Assert.That(loaded.Hangar.Reserve, Is.EqualTo(new[] { 3, 0 }));
            Assert.That(loaded.Hangar.Launched[0].SquadronId, Is.EqualTo(77L));
            Assert.That(loaded.Hangar.Launched[0].Bay, Is.EqualTo(1));
            Assert.That(loaded.Abilities, Is.Empty);
        }

        [Test]
        public void Unit_WithoutHangar_LoadsNoHangar()
        {
            UnitSave unit = UnitSave.Create(5L, new PlayerId(0), UnitTypeId.Squadron(SquadronType.XWing),
                Vector3.zero, Quaternion.identity, UnitHealthSave.ForSquadron(new[] { 1f }, new[] { 1f }),
                UnitOrderSave.From(new UnitOrderModel()));

            UnitSave loaded = JsonUtility.FromJson<UnitSave>(JsonUtility.ToJson(unit));

            Assert.That(loaded.Hangar, Is.Null);
            Assert.That(loaded.UnitType, Is.EqualTo(UnitTypeId.Squadron(SquadronType.XWing)));
        }

        private static UnitOrderSave RoundTrip(UnitOrderSave save) =>
            JsonUtility.FromJson<UnitOrderSave>(JsonUtility.ToJson(save));
    }
}
