using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAssetIntegrityTests
    {
        [TestCase("AcclamatorAssault")]
        [TestCase("VictoryI")]
        [TestCase("VictoryII")]
        [TestCase("ISDIII")]
        public void RepairedShip_EveryHardPointIsTargetableAndHasHealth(string ship)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Models/Ships/Empire/{ship}ShipView.prefab");
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                $"Assets/Settings/Data/Ship/Empire/{ship}ShipData.asset");
            HardPoint[] hardPoints = view.GetComponentsInChildren<HardPoint>(true);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();

            Assert.That(hardPoints, Is.Not.Empty);
            Assert.That(health.ShipUnits, Is.EquivalentTo(hardPoints));
            Assert.That(health.ShipUnits.Distinct().Count(), Is.EqualTo(health.ShipUnits.Count));
            foreach (HardPoint hardPoint in hardPoints)
            {
                HardPointHealth entry = data.HardPointHealth.Single(h => h.HardPointType == hardPoint.HardPointType);
                Assert.That(entry.Health, Is.GreaterThan(0), hardPoint.name);
            }
        }

        [TestCase("Republic", "Arquitens")]
        [TestCase("Republic", "HeavyDreadnought")]
        [TestCase("Separatist", "Munificent")]
        [TestCase("Separatist", "Recusant")]
        public void NonCarrier_HasNoPhantomHangarHealth(string faction, string ship)
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                $"Assets/Settings/Data/Ship/{faction}/{ship}ShipData.asset");
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Models/Ships/{faction}/{ship}ShipView.prefab");

            Assert.That(data.HangarBays, Is.Empty);
            Assert.That(data.HardPointHealth.Any(h => h.HardPointType == HardPointType.Hangar), Is.False);
            Assert.That(view.GetComponentsInChildren<HardPoint>(true)
                .Any(h => h.HardPointType == HardPointType.Hangar), Is.False);
        }

        [Test]
        public void Endurance_ContainsOnlyOperationalBays()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                "Assets/Settings/Data/Ship/Rebellion/EnduranceShipData.asset");

            Assert.That(data.HangarBays.Count, Is.EqualTo(3));
            Assert.That(data.HangarBays.All(b => b.MaxActive > 0 && b.Reserve > 0), Is.True);
        }

        [Test]
        public void Arquitens_UsesItsOwnWreckPrefab()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(
                "Assets/Settings/Data/Ship/Republic/ArquitensShipData.asset");

            Assert.That(data.Wreck, Is.Not.Null);
            Assert.That(data.Wreck.PrefabKey, Is.Not.Empty);
            Assert.That(AssetDatabase.GUIDToAssetPath(data.Wreck.PrefabKey),
                Is.EqualTo("Assets/Prefabs/Models/Wrecks/ArquitensWreckView.prefab"));
        }
    }
}
