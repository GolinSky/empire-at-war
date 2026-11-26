using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyRebuildRolePolicyTests
    {
        private const string FACTION_CATALOG = "Assets/Settings/Data/Factions/Shared/FactionCatalog.asset";

        [TestCase(ShipClass.Frigate, UnitRole.AntiCapital, true)]
        [TestCase(ShipClass.Cruiser, UnitRole.Carrier, true)]
        [TestCase(ShipClass.HeavyCruiser, UnitRole.Artillery, true)]
        [TestCase(ShipClass.HeavyCruiser, UnitRole.Balanced | UnitRole.Dps, true)]
        [TestCase(ShipClass.HeavyCruiser, UnitRole.Brawler, false)]
        [TestCase(ShipClass.Capital, UnitRole.AntiCapital, false)]
        [TestCase(ShipClass.Corvette, UnitRole.AntiCapital, false)]
        public void IsRebuildShip_NeedsAMidSizeClassAndARebuildRole(ShipClass shipClass, UnitRole roles, bool expected)
        {
            Assert.That(EnemyRebuildRolePolicy.IsRebuildShip(shipClass, roles), Is.EqualTo(expected));
        }

        [TestCase(0, false, true)]
        [TestCase(3, false, true)]
        [TestCase(0, true, false)]
        [TestCase(4, false, false)]
        public void IsRebuilding_BelowTheEscortWithNoCapitalToLand(int activeShips, bool capitalLands, bool expected)
        {
            Assert.That(EnemyRebuildRolePolicy.IsRebuilding(activeShips, 4, capitalLands), Is.EqualTo(expected));
        }

        // Every faction can rebuild a wiped fleet: its roster lists at least one rebuild-role warship.
        [Test]
        public void EveryFactionRoster_OffersARebuildShip()
        {
            FactionCatalog catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(FACTION_CATALOG);
            Assert.That(catalog, Is.Not.Null);
            Dictionary<string, ShipData> shipData = AssetDatabase.FindAssets("t:ShipData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(data => data != null)
                .ToDictionary(data => data.name);

            foreach (FactionDefinition faction in catalog.Factions)
            {
                if (faction.Ships.Count == 0) continue;
                bool hasRebuildShip = faction.Ships.Keys.Any(ship =>
                    shipData.TryGetValue(ship + nameof(ShipData), out ShipData data) &&
                    EnemyRebuildRolePolicy.IsRebuildShip(data.ShipClass, data.Roles));
                Assert.That(hasRebuildShip, Is.True, faction.FactionType.ToString());
            }
        }
    }
}
