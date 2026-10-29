using System;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class ShipHeightTierTests
    {
        [Test]
        public void TierOrder_RunsFromDeepToHighest()
        {
            Assert.That(Enum.GetNames(typeof(ShipHeightTier)), Is.EqualTo(new[]
            {
                "Deep", "Lowest", "Low", "LowMid", "Mid", "HighMid", "High", "Highest"
            }));
            Assert.That((int)ShipHeightTier.Deep, Is.Zero);
        }

        [TestCase("AcclamatorAssaultShipData", ShipHeightTier.High, 65f)]
        [TestCase("AcclamatorShipData", ShipHeightTier.High, 65f)]
        [TestCase("ArquitensImperialCruiserShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("ArquitensShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("C9979ShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("CaptorShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("CorellianBattlecruiserShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("CorellianCorvetteShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("DispatcherShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("ExecutorShipData", ShipHeightTier.Deep, -341f)]
        [TestCase("HeavyDreadnoughtShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("HomeOneShipData", ShipHeightTier.Lowest, -241f)]
        [TestCase("ImperatorShipData", ShipHeightTier.Low, -154f)]
        [TestCase("ISDIIIShipData", ShipHeightTier.Low, -154f)]
        [TestCase("ISDIIShipData", ShipHeightTier.Low, -154f)]
        [TestCase("ISDIShipData", ShipHeightTier.Low, -154f)]
        [TestCase("LucrehulkShipData", ShipHeightTier.Lowest, -241f)]
        [TestCase("MalevolenceShipData", ShipHeightTier.Deep, -341f)]
        [TestCase("MandatorShipData", ShipHeightTier.Deep, -520f)]
        [TestCase("MC75ProfundityShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("MC80IndependenceShipData", ShipHeightTier.Lowest, -241f)]
        [TestCase("MonCalCruiserShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("MunificentShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("NebulonBShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("PatrolFrigateShipData", ShipHeightTier.High, 65f)]
        [TestCase("ProvidenceShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("RaiderCorvetteShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("RecusantShipData", ShipHeightTier.Mid, -22f)]
        [TestCase("ResoluteShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("RothanaShipData", ShipHeightTier.Lowest, -241f)]
        [TestCase("StealthCorvetteShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("TectorShipData", ShipHeightTier.Low, -154f)]
        [TestCase("ThrantaShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("VenatorShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("VictoryIIShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("VictoryIShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("VictoryShipData", ShipHeightTier.HighMid, 38f)]
        public void ShipAsset_PreservesTierAndHeight(string assetName, ShipHeightTier expectedTier, float expectedHeight)
        {
            ShipData ship = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(
                System.Array.Find(AssetDatabase.FindAssets(assetName + " t:ShipData", new[] { "Assets/Settings/Data/Ship" }),
                    guid => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == assetName)));
            Assert.That(ship, Is.Not.Null);
            Assert.That(ship.HeightTier, Is.EqualTo(expectedTier));
            Assert.That(ship.Height, Is.EqualTo(expectedHeight));
        }
    }
}
