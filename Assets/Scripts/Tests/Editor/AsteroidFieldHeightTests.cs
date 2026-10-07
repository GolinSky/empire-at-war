using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class AsteroidFieldHeightTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";

        // Navigation drops an obstacle whose height band misses the ship hull span.
        [Test]
        public void FieldBand_BlocksEveryShipExceptDeepAndLowestTiers()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            string[] guids = AssetDatabase.FindAssets("t:ShipData", new[] { "Assets/Settings/Data/Ship" });
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                ShipData ship = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                float bottom = ship.Height + ship.HullBottom;
                float top = ship.Height + ship.HullTop;
                if (ship.HeightTier == ShipHeightTier.Deep || ship.HeightTier == ShipHeightTier.Lowest)
                {
                    Assert.That(top, Is.LessThanOrEqualTo(settings.FieldFloor), $"{ship.name} must pass beneath fields");
                }
                else
                {
                    Assert.That(top, Is.GreaterThan(settings.FieldFloor), $"{ship.name} must be blocked by fields");
                    Assert.That(bottom, Is.LessThan(settings.FieldCeiling), $"{ship.name} must be blocked by fields");
                }
            }
        }
    }
}
