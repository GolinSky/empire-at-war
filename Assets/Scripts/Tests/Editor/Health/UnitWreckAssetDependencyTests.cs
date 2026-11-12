using System.Linq;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.UnitWreck;
using EmpireAtWar.ViewComponents.Wreck;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class UnitWreckAssetDependencyTests
    {
        [TestCase(FactionType.Empire)]
        [TestCase(FactionType.Republic)]
        [TestCase(FactionType.Separatist)]
        public void StationDestruction_SelectsItsFactionWreckForEachLevel(FactionType faction)
        {
            var station = AssetDatabase.LoadAssetAtPath<SpaceStationData>(
                "Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset");
            Assert.That(station.TryGetWrecks(faction, out var wrecks), Is.True);
            Assert.That(wrecks.Levels, Has.Length.EqualTo(5));
            for (int level = 1; level < 5; level++)
                Assert.That(AssetDatabase.GUIDToAssetPath(wrecks.Get(level).PrefabKey),
                    Is.EqualTo($"Assets/Prefabs/Models/Wrecks/{faction}SpaceStationLevel{level}WreckView.prefab"));
            Assert.That(AssetDatabase.GUIDToAssetPath(wrecks.Get(5).PrefabKey),
                Is.EqualTo($"Assets/Prefabs/Models/Wrecks/{faction}SpaceStationWreckView.prefab"));
        }

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
