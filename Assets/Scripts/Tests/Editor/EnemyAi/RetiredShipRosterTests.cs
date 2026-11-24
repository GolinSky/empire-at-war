using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Retired ships keep their data assets but must stay off every faction roster: players and the enemy AI
    /// build only from <see cref="FactionDefinition.Ships"/>.
    /// </summary>
    [Category("EnemyAi")]
    public sealed class RetiredShipRosterTests
    {
        private const string FACTION_CATALOG = "Assets/Settings/Data/Factions/Shared/FactionCatalog.asset";

        private static readonly ShipType[] RETIRED_SHIPS =
        {
            ShipType.Procurator,
            ShipType.Peleset,
            ShipType.Recalcitrant
        };

        [Test]
        public void FactionRosters_ExcludeRetiredShips()
        {
            FactionCatalog catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(FACTION_CATALOG);
            Assert.That(catalog, Is.Not.Null, FACTION_CATALOG);

            foreach (FactionDefinition faction in catalog.Factions)
            foreach (ShipType ship in RETIRED_SHIPS)
                Assert.That(faction.Ships.ContainsKey(ship), Is.False, $"{faction.name} lists {ship}");
        }
    }
}
