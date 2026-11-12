using System;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ExecutorShipTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/ExecutorShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Empire/ExecutorShipData.asset";

        [Test]
        public void SavedShip_HasFullyTargetableHardpoints()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.IgnoresMapObstacles, Is.True);
            Assert.That(data.Abilities, Is.EquivalentTo(new[] { ShipAbilityId.ExecutorLaserBeam, ShipAbilityId.TractorBeam }));
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            HardPoint[] points = view.GetComponentsInChildren<HardPoint>(true);
            Assert.That(points.Select(p => p.Id).Distinct().Count(), Is.EqualTo(points.Length));
            Assert.That(health.ShipUnits.Select(p => p.Id), Is.EqualTo(Enumerable.Range(0, points.Length)));
            Assert.That(health.ShipUnits.Count(p => p.HardPointType == HardPointType.ShieldGenerator), Is.EqualTo(4));
            Assert.That(health.ShipUnits.Count(p => p.HardPointType == HardPointType.Engines), Is.EqualTo(2));
            Assert.That(health.ShipUnits.Count(p => p.HardPointType == HardPointType.Hangar), Is.EqualTo(2));
            Assert.That(weapons.All(health.ShipUnits.Contains), Is.True);
        }

        [Test]
        public void SavedWeaponProfiles_HaveShotPrefabsAndDefenseRoles()
        {
            WeaponsData profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            Assert.That(profiles.GetProfile(WeaponType.HeavyDualMissileLauncher).Interceptable, Is.True);
            Assert.That(profiles.GetProfile(WeaponType.ExecutorPointDefense).StrikecraftOnly, Is.True);
            foreach (WeaponHardPoint weapon in AssetDatabase.LoadAssetAtPath<GameObject>(VIEW).GetComponentsInChildren<WeaponHardPoint>(true))
                Assert.That(profiles.GetProfile(weapon.WeaponType).ShotPrefab, Is.Not.Null, weapon.name);
        }

        [Test]
        public void SavedHangars_ShareTheFighterExitWithInterceptors()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.HangarBays.Select(b => (int)b.SquadronType), Is.EqualTo(new[] { 200, 201, 203 }));
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            SerializedObject hangar = Config(view, "HangarComponent");
            SerializedProperty points = hangar.FindProperty("bayHardPoints");
            SerializedProperty exits = hangar.FindProperty("bayLaunchPoints");
            Assert.That(points.arraySize, Is.EqualTo(3));
            Assert.That(exits.arraySize, Is.EqualTo(3));
            Assert.That(points.GetArrayElementAtIndex(0).objectReferenceValue, Is.Not.EqualTo(points.GetArrayElementAtIndex(1).objectReferenceValue));
            Assert.That(points.GetArrayElementAtIndex(2).objectReferenceValue, Is.EqualTo(points.GetArrayElementAtIndex(0).objectReferenceValue));
            Assert.That(exits.GetArrayElementAtIndex(2).objectReferenceValue, Is.EqualTo(exits.GetArrayElementAtIndex(0).objectReferenceValue));
            for (int i = 0; i < exits.arraySize; i++)
            {
                Transform exit = (Transform)exits.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(exit.position.y - view.transform.position.y, Is.LessThan(data.HullBottom));
            }
        }

        [Test]
        public void SavedRegistrations_ResolveOwnAssetsAndIcons()
        {
            Assert.That((int)ShipType.Executor, Is.EqualTo(207));
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(ShipType.Executor), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (string path in new[] { VIEW, DATA })
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address,
                    Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ExecutorIcon.png");
            Assert.That(Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue").FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(AssetDatabase.GUIDToAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue").FindPropertyRelative("m_AssetGUID").stringValue),
                Is.EqualTo("Assets/Prefabs/Ui/Reinforcement/ExecutorReinforcementView.prefab"));
            ShipAbilityCatalog catalog = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
            Assert.That(catalog.Get(ShipAbilityId.ExecutorLaserBeam).Settings, Is.Not.Null);
            Assert.That(catalog.Get(ShipAbilityId.TractorBeam).Settings, Is.Not.Null);
        }

        [TestCase("Assets/Prefabs/Models/Ships/Empire/Models/Executor.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/ExecutorWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/ExecutorReinforcementView.prefab")]
        public void SavedPrefab_HasBoundVisibleGeometryAndPreservesDisabledHelpers(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Vector3 scale = prefab.transform.localScale;
            Assert.That(scale.x, Is.GreaterThan(0f).And.LessThan(float.PositiveInfinity));
            Assert.That(scale, Is.EqualTo(Vector3.one * scale.x));
            if (path != VIEW && !path.Contains("Wrecks/"))
                Assert.That(scale, Is.EqualTo(Vector3.one));
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), Is.Zero, t.name);
            foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                Assert.That(renderer.sharedMaterials.All(m => m != null), Is.True, renderer.name);
                if (renderer.name.StartsWith("Collision") || renderer.name.StartsWith("Shadow"))
                    Assert.That(renderer.enabled, Is.False, renderer.name);
                if (renderer.enabled)
                    Assert.That(renderer.GetComponents<MeshFilter>().Single().sharedMesh, Is.Not.Null, renderer.name);
            }
        }

        private static SerializedObject Config(GameObject view, string type) =>
            new SerializedObject(view.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == type));

        private static SerializedProperty Entry(string path, string field)
        {
            SerializedProperty array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
            return Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex)
                .Single(p => p.FindPropertyRelative("key").intValue == 207).FindPropertyRelative("value");
        }
    }
}
