using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAbilityCatalogTests
    {
        private const string CATALOG_PATH =
            "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset";

        [Test]
        public void Catalog_ContainsEveryAbilityWithAnIconAndTargetRange()
        {
            ShipAbilityCatalog catalog =
                AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(CATALOG_PATH);
            Assert.That(catalog, Is.Not.Null);

            foreach (ShipAbilityId id in Enum.GetValues(typeof(ShipAbilityId)))
            {
                if (id == ShipAbilityId.None) continue;
                ShipAbilityDefinition definition = catalog.Get(id);
                Assert.That(definition, Is.Not.Null, id.ToString());
                Assert.That(definition.Icon, Is.Not.Null, id.ToString());
                // Null also means the settings class was renamed or moved without [MovedFrom].
                Assert.That(definition.Settings, Is.Not.Null, id.ToString());
                if (definition.RequiresEnemyTarget)
                    Assert.That(definition.Range, Is.GreaterThan(0f), id.ToString());
            }
        }

        [Test]
        public void ConfiguredShips_UseCatalogIdsWithoutDuplicates()
        {
            ShipAbilityCatalog catalog =
                AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(CATALOG_PATH);
            string[] guids = AssetDatabase.FindAssets("t:ShipData",
                new[] { "Assets/Settings/Data/Ship" });
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                ShipData ship = AssetDatabase.LoadAssetAtPath<ShipData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                HashSet<ShipAbilityId> seen = new HashSet<ShipAbilityId>();
                foreach (ShipAbilityId id in ship.Abilities)
                {
                    Assert.That(id, Is.Not.EqualTo(ShipAbilityId.None), ship.name);
                    Assert.That(seen.Add(id), Is.True, ship.name);
                    Assert.That(catalog.Get(id), Is.Not.Null, ship.name);
                }
            }
        }
    }
}
