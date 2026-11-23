using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
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
    /// <summary>Empire Resurgent: battlecruiser brawler, anti-capital and carrier built from Tools/Blender/Resurgent.</summary>
    [Category("Ships")]
    public sealed class ResurgentShipTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/ResurgentShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Empire/ResurgentShipData.asset";
        private const string ISD_II_DATA = "Assets/Settings/Data/Ship/Empire/ISDIIShipData.asset";

        [Test]
        public void SavedShip_HasTargetableHardpointsHangarTractorAndCommandArray()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.ShipClass, Is.EqualTo(ShipClass.BattleCruiser));
            Assert.That(data.Abilities, Is.EquivalentTo(new[] { ShipAbilityId.PowerToMainBatteries, ShipAbilityId.TractorBeam }));
            Assert.That(data.HangarBays, Is.Not.Empty);
            Assert.That(data.HangarBays.All(bay => bay.Reserve > 0 && bay.MaxActive > 0), Is.True);

            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            MonoBehaviour hangar = view.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "HangarComponent");
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            HardPoint[] points = view.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            Assert.That(health.ShipUnits.Select(p => p.Id).OrderBy(id => id), Is.EqualTo(Enumerable.Range(0, points.Length)));
            Assert.That(health.ShipUnits, Is.EquivalentTo(points));
            foreach (HardPointType required in new[] { HardPointType.ShieldGenerator, HardPointType.Engines, HardPointType.TractorBeam,
                         HardPointType.Hangar, HardPointType.CommandArray })
                Assert.That(health.ShipUnits.Any(p => p.HardPointType == required), Is.True, required.ToString());
            var hangarPoint = (HardPoint)new SerializedObject(hangar).FindProperty("hangarHardPoint").objectReferenceValue;
            Assert.That(hangarPoint.HardPointType, Is.EqualTo(HardPointType.Hangar));
            Assert.That(health.ShipUnits.Contains(hangarPoint), Is.True);

            Assert.That(data.WeaponLoadout.Sum(entry => entry.Count), Is.EqualTo(weapons.Length));
            foreach (var group in weapons.GroupBy(w => w.WeaponType))
                Assert.That(data.WeaponLoadout.Single(entry => entry.WeaponType == group.Key).Count, Is.EqualTo(group.Count()), group.Key.ToString());
            Assert.That(weapons.Any(w => new SerializedObject(w).FindProperty("mainBattery").boolValue), Is.True,
                "Power to Main Batteries needs main-battery weapons");
        }

        [Test]
        public void SavedShip_IsABattlecruiserPickedByItsConvexHullWithEnginesAft()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(view.GetComponent<BoxCollider>().enabled, Is.False);
            MeshCollider pick = view.GetComponents<MeshCollider>().Single();
            Assert.That(pick.convex && pick.enabled && pick.sharedMesh != null, Is.True);
            float engineZ = view.GetComponentsInChildren<HardPoint>(true).Where(p => p.HardPointType == HardPointType.Engines)
                .Average(p => view.transform.InverseTransformPoint(p.transform.position).z);
            Assert.That(engineZ, Is.LessThan(0f), "bow must face +Z");
        }

        [Test]
        public void Resurgent_IsStrongerThanTheIsdII()
        {
            ShipData resurgent = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            ShipData isdII = AssetDatabase.LoadAssetAtPath<ShipData>(ISD_II_DATA);
            Assert.That(resurgent.Hull + resurgent.Shields, Is.GreaterThan(isdII.Hull + isdII.Shields));
            WeaponsData weapons = Load<WeaponsData>();
            DamageMatrixData matrix = Load<DamageMatrixData>();
            float VsCapital(ShipData ship) => ship.WeaponLoadout.Sum(entry => entry.Count *
                WeaponEngagementRange.EffectiveDamagePerSecond(weapons.GetProfile(entry.WeaponType), ShipClass.Capital, matrix));
            Assert.That(VsCapital(resurgent), Is.GreaterThan(VsCapital(isdII)));
        }

        [Test]
        public void SavedRegistrations_ResolveOwnAssetsAndIcons()
        {
            Assert.That((int)ShipType.Resurgent, Is.EqualTo(221));
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(ShipType.Resurgent), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (string path in new[] { VIEW, DATA })
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address,
                    Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ResurgentIcon.png");
            Assert.That(icon, Is.Not.Null);
            Assert.That(Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue")
                .FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(AssetDatabase.GUIDToAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue")
                    .FindPropertyRelative("m_AssetGUID").stringValue),
                Is.EqualTo("Assets/Prefabs/Ui/Reinforcement/ResurgentReinforcementView.prefab"));
        }

        [TestCase("Assets/Prefabs/Models/Ships/Empire/Models/Resurgent.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/ResurgentWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/ResurgentReinforcementView.prefab")]
        public void SavedPrefab_HasBoundVisibleGeometryWithoutHelpers(string path)
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
                Assert.That(renderer.name.StartsWith("Collision") || renderer.name.StartsWith("Shadow") ||
                            renderer.name.EndsWith("_Collision"), Is.False, renderer.name);
                if (renderer.enabled)
                    Assert.That(renderer.GetComponents<MeshFilter>().Single().sharedMesh, Is.Not.Null, renderer.name);
            }
        }

        private static T Load<T>() where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:" + typeof(T).Name).Single()));

        private static SerializedProperty Entry(string path, string field)
        {
            SerializedProperty array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
            return Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex)
                .Single(p => p.FindPropertyRelative("key").intValue == (int)ShipType.Resurgent).FindPropertyRelative("value");
        }
    }
}
