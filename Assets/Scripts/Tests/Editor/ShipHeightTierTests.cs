using System;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
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

        [TestCase("AcclamatorAssaultShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("AcclamatorShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("ArquitensImperialCruiserShipData", ShipHeightTier.High, 65f)]
        [TestCase("ArquitensShipData", ShipHeightTier.High, 65f)]
        [TestCase("C9979ShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("CaptorShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("CorellianBattlecruiserShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("CorellianCorvetteShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("DispatcherShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("ExecutorShipData", ShipHeightTier.Deep, -341f)]
        [TestCase("HeavyDreadnoughtShipData", ShipHeightTier.Mid, -22f)]
        [TestCase("HomeOneShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("ImperatorShipData", ShipHeightTier.Low, -154f)]
        [TestCase("ISDIIIShipData", ShipHeightTier.Low, -154f)]
        [TestCase("ISDIIShipData", ShipHeightTier.Low, -154f)]
        [TestCase("ISDIShipData", ShipHeightTier.Low, -154f)]
        [TestCase("LucrehulkShipData", ShipHeightTier.Lowest, -241f)]
        [TestCase("MalevolenceShipData", ShipHeightTier.Deep, -341f)]
        [TestCase("MandatorShipData", ShipHeightTier.Deep, -520f)]
        [TestCase("MC75ProfundityShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("MC80IndependenceShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("MonCalCruiserShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("MunificentShipData", ShipHeightTier.HighMid, 38f)]
        [TestCase("NebulonBShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("PatrolFrigateShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("ProvidenceShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("RaiderCorvetteShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("RecusantShipData", ShipHeightTier.Mid, -22f)]
        [TestCase("ResoluteShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("RothanaShipData", ShipHeightTier.Low, -154f)]
        [TestCase("StealthCorvetteShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("TectorShipData", ShipHeightTier.Low, -154f)]
        [TestCase("ThrantaShipData", ShipHeightTier.Highest, 80f)]
        [TestCase("VenatorShipData", ShipHeightTier.LowMid, -68f)]
        [TestCase("VictoryIIShipData", ShipHeightTier.Low, -154f)]
        [TestCase("VictoryIShipData", ShipHeightTier.Low, -154f)]
        [TestCase("VictoryShipData", ShipHeightTier.Low, -154f)]
        public void ShipAsset_PreservesTierAndHeight(string assetName, ShipHeightTier expectedTier, float expectedHeight)
        {
            ShipData ship = AssetDatabase.LoadAssetAtPath<ShipData>("Assets/Settings/Data/Ship/" + assetName + ".asset");
            Assert.That(ship, Is.Not.Null);
            Assert.That(ship.HeightTier, Is.EqualTo(expectedTier));
            Assert.That(ship.Height, Is.EqualTo(expectedHeight));
        }
    }
}
