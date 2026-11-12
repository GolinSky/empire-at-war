using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ShipSystemHardpointTests
    {
        [TestCase("RecusantShipData")]
        [TestCase("NebulonBShipData")]
        [TestCase("MonCalCruiserShipData")]
        [TestCase("MC80IndependenceShipData")]
        [TestCase("QuasarFireShipData")]
        [TestCase("ArquitensImperialCruiserShipData")]
        [TestCase("ISDIIShipData")]
        public void SavedShip_HasEngineAndShieldGeneratorHardpointsWithHealthAndInjectionBindings(string dataName)
        {
            BalanceUnit unit = BalanceInventory.BuildCombat().Units.Single(candidate =>
                candidate.Data is ShipData && candidate.Data.name == dataName);
            ShipData data = (ShipData)unit.Data;
            var healthBindings = BalancePrefabBindings.References(
                unit.Components.OfType<HealthComponent>().Single(), BalanceRegistration.Auto("ShipUnits"));
            Assert.That(healthBindings.OfType<HardPoint>().Select(point => point.Id).Distinct().Count(),
                Is.EqualTo(healthBindings.Count), unit.Caption);
            var injectables = unit.Components.OfType<EntityBindings>()
                .SelectMany(bindings => BalancePrefabBindings.References(
                    bindings, BalanceRegistration.Auto("Injectables"))).ToArray();

            foreach (HardPointType type in new[] { HardPointType.Engines, HardPointType.ShieldGenerator })
            {
                string context = unit.Caption + " / " + type;
                HardPoint[] hardPoints = unit.Components.OfType<HardPoint>()
                    .Where(point => point.HardPointType == type).ToArray();
                Assert.That(hardPoints, Is.Not.Empty, context);
                Assert.That(healthBindings.OfType<HardPoint>().Where(point => point.HardPointType == type),
                    Is.EquivalentTo(hardPoints), context);
                Assert.That(injectables.OfType<HardPoint>().Where(point => point.HardPointType == type),
                    Is.EquivalentTo(hardPoints), context);
                Assert.That(data.UsesHullHardPointBudget, Is.True, context);
                Assert.That(hardPoints.All(point => point.HealthWeight > 0f), Is.True, context);

                foreach (HardPoint point in hardPoints)
                {
                    string pointContext = context + " / " + point.name;
                    Assert.That(point.UnlockLevel, Is.EqualTo(1), pointContext);
                    SerializedObject serialized = new SerializedObject(point);
                    Transform anchor = serialized.FindProperty("destroyedVfxAnchor").objectReferenceValue as Transform;
                    Assert.That(anchor, Is.Not.Null, pointContext);
                    Assert.That(anchor.root, Is.EqualTo(point.transform.root), pointContext);
                    Assert.That(serialized.FindProperty("destroyedVfxScale").floatValue,
                        Is.GreaterThan(0f), pointContext);
                }
            }
        }
    }
}
