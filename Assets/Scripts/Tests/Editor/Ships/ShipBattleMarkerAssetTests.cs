using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ShipBattleMarkerAssetTests
    {
        private const string SHIPS_DATA = "Assets/Settings/Data/Ship/Shared/ShipsData.asset";
        private const string SHIP_UI_DATA = "Assets/Settings/Data/Models/ShipUi/ShipUiData.asset";

        [TestCase(ShipType.RaiderCorvette)]
        [TestCase(ShipType.Tartan)]
        [TestCase(ShipType.CorellianCorvette)]
        [TestCase(ShipType.CD273)]
        [TestCase(ShipType.CR70)]
        [TestCase(ShipType.Thranta)]
        [TestCase(ShipType.Carrack)]
        [TestCase(ShipType.StealthCorvette)]
        [TestCase(ShipType.PatrolFrigate)]
        [TestCase(ShipType.C9979)]
        [TestCase(ShipType.Centax)]
        [TestCase(ShipType.Pelta)]
        public void SmallShip_OptsInAndUsesItsCatalogIcon(ShipType shipType)
        {
            Assert.That(GetShipData(shipType).ShowBattleMarker, Is.True);
            var icons = AssetDatabase.LoadAssetAtPath<ShipUiData>(SHIP_UI_DATA);
            Assert.That(AssetDatabase.GetAssetPath(icons.GetShipIcon(shipType)),
                Does.EndWith($"/{shipType}Icon.png"));
        }

        [TestCase(ShipType.Venator)]
        [TestCase(ShipType.NebulonB)]
        public void OtherShip_HasBattleMarkerDisabled(ShipType shipType)
        {
            Assert.That(GetShipData(shipType).ShowBattleMarker, Is.False);
        }

        private static ShipData GetShipData(ShipType shipType)
        {
            var ships = AssetDatabase.LoadAssetAtPath<ShipsData>(SHIPS_DATA);
            return AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(ships.GetShipDataPath(shipType)));
        }
    }
}
