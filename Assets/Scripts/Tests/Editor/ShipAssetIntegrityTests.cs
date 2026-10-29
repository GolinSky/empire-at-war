using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ShipAssetIntegrityTests
    {
        [Test]
        public void SavedShips_InstalledHardpointsCoverHullBudgetWithoutFlatOverrides()
        {
            string settingsPath = AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:HardPointHealthSettings").Single());
            HardPointHealthBudget budget = AssetDatabase.LoadAssetAtPath<HardPointHealthSettings>(settingsPath).CreateBudget();
            foreach (BalanceUnit unit in BalanceInventory.BuildCombat().Units.Where(unit => unit.Data is ShipData))
            {
                ShipData data = (ShipData)unit.Data;
                Assert.That(new SerializedObject(data).FindProperty("hardPointHealth"), Is.Null, unit.Caption);
                HardPoint[] mounts = unit.Components.OfType<HealthComponent>().Single().ShipUnits.ToArray();
                if (mounts.Length == 0) continue;
                Assert.That(mounts.All(point => point.HealthOverride == 0f), Is.True, unit.Caption);
                HardPointModel[] models = mounts.Select(point => new HardPointModel(point.HardPointType,
                    point.Id, point.UnlockLevel, healthWeight: point.HealthWeight)).ToArray();
                float[] health = budget.Calculate(data.Hull, data.ShipClass, models);
                Assert.That(health.Sum(), Is.GreaterThanOrEqualTo(data.Hull * HardPointHealthBudget.DEFAULT_BUDGET_RATIO - 0.1f),
                    unit.Caption);
                for (int i = 0; i < models.Length; i++)
                    if (models[i].UnlockLevel <= 1)
                        Assert.That(health[i], Is.GreaterThan(0f), unit.Caption + " / " + mounts[i].name);
            }
        }

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
                Assert.That(hardPoint.HealthWeight, Is.GreaterThan(0f), hardPoint.name);
                Assert.That(hardPoint.HealthOverride, Is.Zero, hardPoint.name);
            }
        }

        [TestCase("Empire", "Victory")]
        [TestCase("Empire", "VictoryI")]
        [TestCase("Empire", "VictoryII")]
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
            Assert.That(data.UsesHullHardPointBudget, Is.True);
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
