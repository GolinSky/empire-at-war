using System;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class ISDIIShipTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/ISDIIShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Empire/ISDIIShipData.asset";

        [Test]
        public void SavedShip_HasTargetableWeaponsAndSystems()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.Abilities, Is.EquivalentTo(new[] { ShipAbilityId.PowerToMainBatteries, ShipAbilityId.TractorBeam }));
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            HardPoint[] hardPoints = view.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            Assert.That(hardPoints.Select(h => h.Id).Distinct().Count(), Is.EqualTo(hardPoints.Length));
            Assert.That(health.ShipUnits, Is.EquivalentTo(hardPoints));
            Assert.That(health.ShipUnits.Count(h => h.HardPointType == HardPointType.Engines), Is.EqualTo(1));
            Assert.That(health.ShipUnits.Count(h => h.HardPointType == HardPointType.ShieldGenerator), Is.EqualTo(1));
            Assert.That(weapons.All(health.ShipUnits.Contains), Is.True);
            MonoBehaviour hangar = Component(view, "HangarComponent");
            SerializedObject hangarConfig = new SerializedObject(hangar);
            Assert.That(hangarConfig.FindProperty("hangarHardPoint").objectReferenceValue, Is.EqualTo(health.ShipUnits.Single(h => h.HardPointType == HardPointType.Hangar)));
            Transform launch = (Transform)hangarConfig.FindProperty("launchPoint").objectReferenceValue;
            BoxCollider collider = view.GetComponents<BoxCollider>().Single();
            Assert.That(new Bounds(collider.center, collider.size).Contains(
                collider.transform.InverseTransformPoint(launch.position)), Is.False);
        }

        [Test]
        public void SavedBroadsideBatteries_CoverTheirOwnBroadside()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            WeaponHardPoint[] batteries = view.GetComponentsInChildren<WeaponHardPoint>(true)
                .Where(w => w.WeaponType == WeaponType.ISDIIOctupleTurbolaser || w.WeaponType == WeaponType.ISDIIQuadIonCannon).ToArray();
            Assert.That(batteries.Count(w => w.transform.localPosition.x < 0), Is.EqualTo(5));
            Assert.That(batteries.Count(w => w.transform.localPosition.x > 0), Is.EqualTo(5));
            foreach (WeaponHardPoint battery in batteries)
            {
                float side = battery.transform.localPosition.x < 0 ? -90f : 90f;
                Assert.That((battery.MinYaw + battery.MaxYaw) / 2f, Is.EqualTo(side), battery.name);
                Assert.That(battery.MaxYaw - battery.MinYaw,
                    Is.EqualTo(battery.WeaponType == WeaponType.ISDIIOctupleTurbolaser ? 90f : 80f), battery.name);
            }
        }

        [Test]
        public void SavedRegistrations_ResolveOwnViewsIconsAndAbility()
        {
            Assert.That((int)ShipType.ISDII, Is.EqualTo(205));
            string guid = AssetDatabase.AssetPathToGUID(DATA);
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(ShipType.ISDII), Is.EqualTo(guid));
            foreach (string path in new[] { DATA, VIEW })
            {
                var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.That(entry.address, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            }
            SerializedProperty faction = Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue");
            Assert.That(faction.FindPropertyRelative("<Name>k__BackingField").stringValue, Is.EqualTo("ISD II"));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ISDIIIcon.png");
            Assert.That(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue").objectReferenceValue, Is.EqualTo(icon));
            string spawn = AssetDatabase.GUIDToAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue").FindPropertyRelative("m_AssetGUID").stringValue);
            Assert.That(spawn, Is.EqualTo("Assets/Prefabs/Ui/Reinforcement/ISDIIReinforcementView.prefab"));
            ShipAbilityCatalog catalog = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
            ShipAbilityDefinition definition = catalog.Get(ShipAbilityId.PowerToMainBatteries);
            Assert.That(definition.Settings, Is.TypeOf<PowerToMainBatteriesSettings>());
            WeaponsData profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            // Main batteries are Heavy Octuple / Quad mounts: the family's salvo, damage scaled by the mount rule.
            WeaponProfile heavyTurbolaser = profiles.GetProfile(WeaponType.HeavyTurboLaser);
            WeaponProfile octuple = profiles.GetProfile(WeaponType.ISDIIOctupleTurbolaser);
            Assert.That(octuple.ShotsPerSalvo, Is.EqualTo(heavyTurbolaser.ShotsPerSalvo));
            Assert.That(octuple.Damage, Is.EqualTo(heavyTurbolaser.Damage * profiles.GetMount(WeaponMount.Octuple).DamageMultiplier).Within(0.01f));
            Assert.That(octuple.Range, Is.EqualTo(heavyTurbolaser.Range).Within(0.01f));
            Assert.That(profiles.GetProfile(WeaponType.ISDIIQuadIonCannon).Damage,
                Is.EqualTo(profiles.GetProfile(WeaponType.HeavyIonCannon).Damage * profiles.GetMount(WeaponMount.Quad).DamageMultiplier).Within(0.01f));
            Assert.That(profiles.GetProfile(WeaponType.ISDIIQuadIonCannon).Range,
                Is.EqualTo(profiles.GetProfile(WeaponType.HeavyIonCannon).Range).Within(0.01f));
        }

        [TestCase("Assets/Prefabs/Models/Ships/Empire/Models/ISDII.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/ISDIIWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/ISDIIReinforcementView.prefab")]
        public void SavedPrefab_HasNoMissingScriptsOrUnboundVisibleGeometry(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Vector3 scale = prefab.transform.localScale;
            Assert.That(scale.x, Is.GreaterThan(0f).And.LessThan(float.PositiveInfinity));
            Assert.That(scale, Is.EqualTo(Vector3.one * scale.x));
            // Deploy previews are scaled to match the deployed ship view (cdc68d97a).
            if (path.Contains("/Reinforcement/"))
                Assert.That(scale, Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(VIEW).transform.localScale));
            else if (path != VIEW && !path.Contains("Wrecks/"))
                Assert.That(scale, Is.EqualTo(Vector3.one));
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), Is.Zero, t.name);
            foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
            {
                Assert.That(renderer.GetComponents<MeshFilter>().Single().sharedMesh, Is.Not.Null, renderer.name);
                Assert.That(renderer.sharedMaterials, Is.Not.Empty);
                Assert.That(renderer.sharedMaterials.All(m => m != null), Is.True, renderer.name);
                if (path.Contains("Wrecks/ISDII"))
                    Assert.That(renderer.sharedMaterials.All(m => !m.name.Contains("Advanced")), Is.True, renderer.name);
            }
        }

        private static MonoBehaviour Component(GameObject view, string type) =>
            view.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == type);

        private static SerializedProperty Entry(string path, string field)
        {
            SerializedProperty array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
            return Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex)
                .Single(p => p.FindPropertyRelative("key").intValue == 205).FindPropertyRelative("value");
        }
    }
}
