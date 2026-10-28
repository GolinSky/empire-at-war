using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class MC30cPrefabTests
    {
        [TestCase("MC30c")]
        [TestCase("MC50")]
        [TestCase("AssaultFrigateMkII")]
        [TestCase("CD273")]
        public void ShipWithoutHangarBays_DoesNotContainAnInjectableHangarComponent(string shipName)
        {
            var data = AssetDatabase.LoadAssetAtPath<ShipData>($"Assets/Settings/Data/Ship/Rebellion/{shipName}ShipData.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Models/Ships/Rebellion/{shipName}ShipView.prefab");

            Assert.That(data.HangarBays, Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<HangarComponent>(true), Is.Empty,
                "Ships without bays must omit the hangar component and its required model dependency.");
        }
    }
}
