using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class HazardHeightTests
    {
        [Test]
        public void HazardBand_ContainsEveryShipHull()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            string[] guids = AssetDatabase.FindAssets("t:ShipData", new[] { "Assets/Settings/Data/Ship" });
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                ShipData ship = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(ship.Height + ship.HullBottom, Is.GreaterThanOrEqualTo(settings.HazardFloor), ship.name);
                Assert.That(ship.Height + ship.HullTop, Is.LessThanOrEqualTo(settings.HazardCeiling), ship.name);
            }
        }
    }
}
