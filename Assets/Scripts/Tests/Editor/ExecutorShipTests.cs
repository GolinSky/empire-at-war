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
    public sealed class ExecutorShipTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/ExecutorShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/ExecutorShipData.asset";

        [Test]
        public void SavedShip_MatchesRequestedStatsAndTargetability()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.Hull, Is.EqualTo(200000));
            Assert.That(data.Shields, Is.EqualTo(200000));
            Assert.That(data.Speed, Is.EqualTo(15));
            Assert.That(data.Abilities, Is.EquivalentTo(new[] { ShipAbilityId.LaserBeam, ShipAbilityId.TractorBeam }));
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            HardPoint[] points = view.GetComponentsInChildren<HardPoint>(true);
            Assert.That(points.Select(p => p.Id).Distinct().Count(), Is.EqualTo(182));
            Assert.That(health.ShipUnits.Count, Is.EqualTo(59));
            Assert.That(health.ShipUnits.Select(p => p.Id), Is.EqualTo(Enumerable.Range(0, 59)));
            Assert.That(health.ShipUnits.Count(p => p.HardPointType == HardPointType.ShieldGenerator), Is.EqualTo(4));
            Assert.That(health.ShipUnits.Count(p => p.HardPointType == HardPointType.Engines), Is.EqualTo(2));
            Assert.That(health.ShipUnits.Count(p => p.HardPointType == HardPointType.Hangar), Is.EqualTo(2));
            Assert.That(weapons.Length, Is.EqualTo(174));
            foreach (var expected in new[] { (49, 10), (50, 23), (51, 8), (52, 10), (53, 69), (28, 40), (54, 14) })
            {
                WeaponHardPoint[] group = weapons.Where(w => (int)w.WeaponType == expected.Item1).ToArray();
                Assert.That(group.Length, Is.EqualTo(expected.Item2));
                Assert.That(group.All(health.ShipUnits.Contains), Is.EqualTo(expected.Item1 >= 49 && expected.Item1 <= 52));
                foreach (WeaponHardPoint weapon in group)
                    Assert.That(weapon.MaxYaw - weapon.MinYaw, Is.InRange(179f, 180f), weapon.name);
            }
        }

        [Test]
        public void SavedWeaponProfiles_HaveRequiredBurstsAndDoNotChangeOtherShips()
        {
            WeaponsData profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            foreach (int type in new[] { 49, 50, 51, 53 })
                Assert.That(profiles.GetProfile((WeaponType)type).ShotsPerSalvo, Is.EqualTo(2));
            Assert.That(profiles.GetProfile(WeaponType.HeavyBurstArtilleryRocket).ShotsPerSalvo, Is.EqualTo(8));
            Assert.That(profiles.GetProfile(WeaponType.HeavyBurstArtilleryRocket).Interceptable, Is.True);
            Assert.That(profiles.GetProfile(WeaponType.RepeatingPointDefense).StrikecraftOnly, Is.True);
            Assert.That(profiles.GetProfile(WeaponType.HeavyArtilleryRocket).ShotsPerSalvo, Is.EqualTo(4));
            foreach (WeaponHardPoint weapon in AssetDatabase.LoadAssetAtPath<GameObject>(VIEW).GetComponentsInChildren<WeaponHardPoint>(true))
                Assert.That(profiles.GetProfile(weapon.WeaponType).ShotPrefab, Is.Not.Null, weapon.name);
        }

        [Test]
        public void SavedHangars_HaveSeparateSystemsExitsAndIndependentReserves()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.HangarBays.Select(b => (int)b.SquadronType), Is.EqualTo(new[] { 200, 201 }));
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            SerializedObject hangar = Config(view, "HangarComponent");
            SerializedProperty points = hangar.FindProperty("bayHardPoints");
            SerializedProperty exits = hangar.FindProperty("bayLaunchPoints");
            Assert.That(points.arraySize, Is.EqualTo(2));
            Assert.That(exits.arraySize, Is.EqualTo(2));
            Assert.That(points.GetArrayElementAtIndex(0).objectReferenceValue, Is.Not.EqualTo(points.GetArrayElementAtIndex(1).objectReferenceValue));
            for (int i = 0; i < 2; i++)
            {
                Transform exit = (Transform)exits.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(exit.localPosition.y, Is.LessThan(data.HullBottom));
            }
            HangarModel model = new HangarModel(data);
            model.DisableBay(0);
            Assert.That(model.TryLaunch(data.HangarInitialDelay, out int bay), Is.True);
            Assert.That(bay, Is.EqualTo(1));
            Assert.That(model.GetReserve(0), Is.EqualTo(6));
            model.DisableBay(1);
            Assert.That(model.IsOperational, Is.False);
            Assert.That(model.TryLaunch(100, out _), Is.False);
        }

        [Test]
        public void SavedRegistrations_ResolveOwnAssetsAndIcons()
        {
            Assert.That((int)ShipType.Executor, Is.EqualTo(207));
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(ShipType.Executor), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (string path in new[] { VIEW, DATA })
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address,
                    Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ExecutorIcon.png");
            Assert.That(Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue").FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(AssetDatabase.GetAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue").objectReferenceValue),
                Is.EqualTo("Assets/Prefabs/Ui/Reinforcement/ExecutorReinforcementView.prefab"));
            ShipAbilityCatalog catalog = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
            Assert.That(catalog.Get(ShipAbilityId.LaserBeam).Settings, Is.Not.Null);
            Assert.That(catalog.Get(ShipAbilityId.TractorBeam).Settings, Is.Not.Null);
        }

        [TestCase("Assets/Prefabs/Models/Ships/Executor.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/ExecutorWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/ExecutorReinforcementView.prefab")]
        public void SavedPrefab_HasBoundVisibleGeometryAndPreservesDisabledHelpers(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
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
