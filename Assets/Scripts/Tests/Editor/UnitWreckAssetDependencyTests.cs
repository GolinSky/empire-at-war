using System.Linq;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.ViewComponents.Wreck;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitWreckAssetDependencyTests
    {
        [Test]
        public void WreckData_ReferencesAddressablePrefabsWithoutHardDependencies()
        {
            string[] wreckDataGuids = AssetDatabase.FindAssets("t:UnitWreckData");
            Assert.That(wreckDataGuids, Is.Not.Empty);
            foreach (string guid in wreckDataGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnitWreckData data = AssetDatabase.LoadAssetAtPath<UnitWreckData>(path);
                string prefabPath = AssetDatabase.GUIDToAssetPath(data.PrefabKey);
                Assert.That(prefabPath, Does.StartWith("Assets/Prefabs/Models/Wrecks/"), path);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab.TryGetComponent(out UnitWreckView _), Is.True, path);
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(data.PrefabKey).parentGroup.Name,
                    Does.EndWith("UnitViews").Or.EqualTo("View"), path);
                Assert.That(AssetDatabase.GetDependencies(path, true), Does.Not.Contain(prefabPath), path);
            }
        }

        [Test]
        public void ShipAndStationData_DoNotDependOnWreckPrefabs()
        {
            string[] dataGuids = AssetDatabase.FindAssets("t:ShipData")
                .Concat(AssetDatabase.FindAssets("t:SpaceStationData")).ToArray();
            Assert.That(dataGuids, Is.Not.Empty);
            foreach (string guid in dataGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Assert.That(AssetDatabase.GetDependencies(path, true)
                    .Where(dependency => dependency.StartsWith("Assets/Prefabs/Models/Wrecks/")),
                    Is.Empty, path);
            }
        }
    }
}
