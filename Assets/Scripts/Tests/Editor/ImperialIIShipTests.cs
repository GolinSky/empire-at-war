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
    public sealed class ImperialIIShipTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/ImperialIIShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/ImperialIIShipData.asset";

        [Test]
        public void SavedShip_HasRequestedStatsWeaponsAndTargetableSystems()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.Hull, Is.EqualTo(21000));
            Assert.That(data.Shields, Is.EqualTo(18000));
            Assert.That(data.Speed, Is.EqualTo(25));
            Assert.That(data.Abilities, Is.EquivalentTo(new[] { ShipAbilityId.PowerToMainBatteries, ShipAbilityId.TractorBeam }));
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            HardPoint[] hardPoints = view.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            Assert.That(hardPoints.Select(h => h.Id).Distinct().Count(), Is.EqualTo(30));
            Assert.That(health.ShipUnits.Count, Is.EqualTo(17));
            Assert.That(health.ShipUnits.Count(h => h.HardPointType == HardPointType.Engines), Is.EqualTo(3));
            Assert.That(health.ShipUnits.Count(h => h.HardPointType == HardPointType.ShieldGenerator), Is.EqualTo(2));
            Assert.That(weapons.Length, Is.EqualTo(23));
            foreach (var expected in new[] { (WeaponType.LightRapidDualTurbolaser, 8), (WeaponType.MediumLongRangeTurboIon, 2),
                (WeaponType.MediumTurboLaser, 3), (WeaponType.LightTurbolaser, 6), (WeaponType.HeavyLaser, 4) })
                Assert.That(weapons.Count(w => w.WeaponType == expected.Item1), Is.EqualTo(expected.Item2));
            Assert.That(weapons.Count(w => new SerializedObject(w).FindProperty("mainBattery").boolValue), Is.EqualTo(8));
            Assert.That(weapons.Where(w => w.WeaponType == WeaponType.LightRapidDualTurbolaser || w.WeaponType == WeaponType.MediumLongRangeTurboIon)
                .All(health.ShipUnits.Contains), Is.True);
            Assert.That(weapons.Where(w => w.WeaponType == WeaponType.MediumTurboLaser || w.WeaponType == WeaponType.LightTurbolaser || w.WeaponType == WeaponType.HeavyLaser)
                .Any(health.ShipUnits.Contains), Is.False);
            MonoBehaviour hangar = Component(view, "HangarComponent");
            SerializedObject hangarConfig = new SerializedObject(hangar);
            Assert.That(hangarConfig.FindProperty("hangarHardPoint").objectReferenceValue, Is.EqualTo(health.ShipUnits.Single(h => h.HardPointType == HardPointType.Hangar)));
            Transform launch = (Transform)hangarConfig.FindProperty("launchPoint").objectReferenceValue;
            BoxCollider collider = view.GetComponents<BoxCollider>().Single();
            Assert.That(new Bounds(collider.center, collider.size).Contains(launch.localPosition), Is.False);
        }

        [Test]
        public void SavedLongRangeBatteries_CoverTheirOwnBroadside()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            WeaponHardPoint[] batteries = view.GetComponentsInChildren<WeaponHardPoint>(true)
                .Where(w => w.WeaponType == WeaponType.LightRapidDualTurbolaser || w.WeaponType == WeaponType.MediumLongRangeTurboIon).ToArray();
            Assert.That(batteries.Count(w => w.transform.localPosition.x < 0), Is.EqualTo(5));
            Assert.That(batteries.Count(w => w.transform.localPosition.x > 0), Is.EqualTo(5));
            foreach (WeaponHardPoint battery in batteries)
            {
                float side = battery.transform.localPosition.x < 0 ? -90f : 90f;
                Assert.That((battery.MinYaw + battery.MaxYaw) / 2f, Is.EqualTo(side), battery.name);
                Assert.That(battery.MaxYaw - battery.MinYaw,
                    Is.EqualTo(battery.WeaponType == WeaponType.LightRapidDualTurbolaser ? 100f : 80f), battery.name);
            }
        }

        [Test]
        public void SavedHangar_LaunchesEachRequestedSquadronAndOneReplacement()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.HangarBays.Select(b => (int)b.SquadronType), Is.EqualTo(new[] { 203, 204, 205 }));
            HangarModel model = new HangarModel(data);
            for (int expected = 0; expected < 3; expected++)
            {
                Assert.That(model.TryLaunch(30f, out int bay), Is.True);
                Assert.That(bay, Is.EqualTo(expected));
                Assert.That(model.GetReserve(bay), Is.EqualTo(1));
                Assert.That(model.GetActive(bay), Is.EqualTo(1));
            }
            Assert.That(model.TryLaunch(30f, out _), Is.False);
            for (int expected = 0; expected < 3; expected++)
            {
                model.SquadronLost(expected);
                Assert.That(model.TryLaunch(30f, out int bay), Is.True);
                Assert.That(bay, Is.EqualTo(expected));
                Assert.That(model.GetReserve(bay), Is.Zero);
            }
            model.SquadronLost(0);
            Assert.That(model.TryLaunch(30f, out _), Is.False);
            model.Shutdown();
            Assert.That(model.TryLaunch(30f, out _), Is.False);
        }

        [Test]
        public void SavedRegistrations_ResolveOwnViewsIconsAndAbility()
        {
            Assert.That((int)ShipType.ImperialII, Is.EqualTo(205));
            string guid = AssetDatabase.AssetPathToGUID(DATA);
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(ShipType.ImperialII), Is.EqualTo(guid));
            foreach (string path in new[] { DATA, VIEW })
            {
                var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.That(entry.address, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            }
            SerializedProperty faction = Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue");
            Assert.That(faction.FindPropertyRelative("<Name>k__BackingField").stringValue, Is.EqualTo("Imperial II Star Destroyer"));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ImperialIIIcon.png");
            Assert.That(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue").objectReferenceValue, Is.EqualTo(icon));
            UnityEngine.Object spawn = Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue").objectReferenceValue;
            Assert.That(AssetDatabase.GetAssetPath(spawn), Is.EqualTo("Assets/Prefabs/Ui/Reinforcement/ImperialIIReinforcementView.prefab"));
            ShipAbilityCatalog catalog = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
            ShipAbilityDefinition definition = catalog.Get(ShipAbilityId.PowerToMainBatteries);
            Assert.That(definition.Settings, Is.TypeOf<PowerToMainBatteriesSettings>());
            Assert.That(definition.Duration, Is.EqualTo(20f));
            Assert.That(definition.RecoveryDelay, Is.EqualTo(60f));
            WeaponsData profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            Assert.That(profiles.GetProfile(WeaponType.LightRapidDualTurbolaser).ShotsPerSalvo, Is.EqualTo(4));
            Assert.That(profiles.GetProfile(WeaponType.MediumLongRangeTurboIon).ShotsPerSalvo, Is.EqualTo(2));
            Assert.That(profiles.GetProfile(WeaponType.LightRapidDualTurbolaser).Range, Is.EqualTo(450));
            Assert.That(profiles.GetProfile(WeaponType.MediumLongRangeTurboIon).Range, Is.EqualTo(525));
        }

        [TestCase("Assets/Prefabs/Models/Ships/ImperialII.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/ImperialIIWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/ImperialIIReinforcementView.prefab")]
        public void SavedPrefab_HasNoMissingScriptsOrUnboundVisibleGeometry(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), Is.Zero, t.name);
            foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
            {
                Assert.That(renderer.GetComponents<MeshFilter>().Single().sharedMesh, Is.Not.Null, renderer.name);
                Assert.That(renderer.sharedMaterials, Is.Not.Empty);
                Assert.That(renderer.sharedMaterials.All(m => m != null), Is.True, renderer.name);
                if (path.Contains("Wrecks/ImperialII"))
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
