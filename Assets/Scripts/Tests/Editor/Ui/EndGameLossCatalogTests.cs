using System;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EndGameUi;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [TestFixture, Category("Ui")]
    public sealed class EndGameLossCatalogTests
    {
        private const string CATALOG_FOLDER = "Assets/Settings/Data/Factions/Shared/";
        private const string SITE_DATA = "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset";

        private EndGameLossCatalog _catalog;

        [SetUp]
        public void SetUp() => _catalog = new EndGameLossCatalog(null, null, null,
            AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(CATALOG_FOLDER + "DefendPlatformCatalog.asset"),
            AssetDatabase.LoadAssetAtPath<MiningFacilityCatalog>(CATALOG_FOLDER + "MiningFacilityCatalog.asset"),
            AssetDatabase.LoadAssetAtPath<CaptureSiteData>(SITE_DATA));

        [Test]
        public void Describe_ResolvesEveryMiningFacilityType()
        {
            foreach (MiningFacilityType type in Enum.GetValues(typeof(MiningFacilityType)))
                Assert.DoesNotThrow(() => _catalog.Describe(
                    new[] { new BattleUnitLoss(BattleLossKind.MiningFacility, (int)type, 1) }), type.ToString());
        }

        [Test]
        public void Describe_ResolvesEveryDefendPlatformType()
        {
            foreach (DefendPlatformType type in Enum.GetValues(typeof(DefendPlatformType)))
                Assert.DoesNotThrow(() => _catalog.Describe(
                    new[] { new BattleUnitLoss(BattleLossKind.DefendPlatform, (int)type, 1) }), type.ToString());
        }

        [Test]
        public void Describe_NamesShipsAndSquadronsThatNoRosterFields()
        {
            // Old saves keep losses of units since retired from every roster.
            var factions = AssetDatabase.LoadAssetAtPath<FactionCatalog>(CATALOG_FOLDER + "FactionCatalog.asset");
            var catalog = new EndGameLossCatalog(factions, new NoIcons(), null, null, null, null);
            foreach (ShipType type in Enum.GetValues(typeof(ShipType)))
                if (!factions.TryGetShipFactionData(type, out _))
                    Assert.That(catalog.Describe(new[] { new BattleUnitLoss(BattleLossKind.Ship, (int)type, 1) })[0].Name,
                        Is.Not.Empty, type.ToString());
            foreach (SquadronType type in Enum.GetValues(typeof(SquadronType)))
                if (!factions.TryGetSquadronFactionData(type, out _))
                    Assert.That(catalog.Describe(new[] { new BattleUnitLoss(BattleLossKind.Squadron, (int)type, 1) })[0].Name,
                        Is.Not.Empty, type.ToString());
        }

        private sealed class NoIcons : IShipIconProvider
        {
            public Sprite GetShipIcon(ShipType shipType) => null;
            public Sprite GetSquadronIcon(SquadronType squadronType) => null;
        }
    }
}
