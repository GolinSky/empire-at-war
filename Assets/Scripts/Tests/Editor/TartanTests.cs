using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class TartanTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/TartanShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Empire/TartanShipData.asset";

        [Test]
        public void SavedUnit_HasRequestedStatsAndNoHangar()
        {
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.Hull, Is.EqualTo(600));
            Assert.That(data.Shields, Is.EqualTo(600));
            Assert.That(data.Speed, Is.EqualTo(70));
            Assert.That(data.ShipClass, Is.EqualTo(ShipClass.Corvette));
            Assert.That(data.Abilities, Is.EqualTo(new[] {ShipAbilityId.TIEAvengerPowerToWeapons}));
            Assert.That(data.HangarBays, Is.Empty);
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(view.GetComponentsInChildren<MonoBehaviour>(true).Any(m => m.GetType().Name == "HangarComponent"), Is.False);
            var roster = Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue");
            Assert.That(roster.FindPropertyRelative("<Price>k__BackingField").intValue, Is.EqualTo(1200));
            Assert.That(roster.FindPropertyRelative("<BuildTime>k__BackingField").intValue, Is.EqualTo(28));
            Assert.That(roster.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue, Is.EqualTo(1));
        }

        [Test]
        public void Weapons_AreTargetableAndBoundToTheirSourceMuzzles()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            Assert.That(weapons.Length, Is.EqualTo(9));
            Assert.That(weapons.Select(w => w.Id).Distinct().Count(), Is.EqualTo(9));
            Assert.That(view.GetComponentsInChildren<HealthComponent>(true).Single().ShipUnits, Is.EquivalentTo(view.GetComponentsInChildren<HardPoint>(true)));
            var bindings = view.GetComponents<EntityBindings>().Single();
            Assert.That(bindings.Injectables, Is.All.Not.Null);
            Assert.That(bindings.Injectables.Distinct().Count(), Is.EqualTo(12));
            Assert.That(bindings.Injectables.OfType<WeaponHardPoint>(), Is.EquivalentTo(weapons));
            var profiles = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            foreach (var expected in new[] {(WeaponType.Laser, 7), (WeaponType.PointDefense, 2)})
            {
                Assert.That(weapons.Count(w => w.WeaponType == expected.Item1), Is.EqualTo(expected.Item2));
                Assert.That(profiles.GetProfile(expected.Item1).ShotPrefab, Is.Not.Null);
            }
            Assert.That(profiles.GetProfile(WeaponType.PointDefense).StrikecraftOnly, Is.True);
            Assert.That(weapons.Single(w => w.Id == 7).name, Is.EqualTo("Tartan_Laser_HP_05"));
            Assert.That(weapons.Single(w => w.Id == 8).name, Is.EqualTo("Tartan_Laser_HP_06"));
            var bones = view.GetComponentsInChildren<Transform>(true);
            string[] muzzles = {"MuzzleA_00", "HP_01", "HP_02", "HP_03", "HP_05", "HP_01", "HP_02", "HP_05", "HP_06"};
            foreach (var weapon in weapons.OrderBy(w => w.Id))
            {
                Assert.That(weapon.HardPointType, Is.EqualTo(HardPointType.Weapon));
                var muzzle = bones.Single(t => t.name == muzzles[weapon.Id] && t.GetComponents<MeshFilter>().Length == 0);
                Assert.That(Vector3.Distance(weapon.transform.position, muzzle.position), Is.LessThan(.0001f), weapon.name);
                Assert.That(weapon.MaxYaw - weapon.MinYaw, Is.EqualTo(weapon.Id == 0 ? 30 : weapon.Id is >= 1 and <= 4 ? 150 : 360).Within(.001f));
                if (weapon.Id is >= 1 and <= 4)
                    Assert.That(Mathf.Sign((weapon.MinYaw + weapon.MaxYaw) * .5f), Is.EqualTo(Mathf.Sign(weapon.transform.localPosition.x)), weapon.name + " must face away from the hull");
            }
        }

        [Test]
        public void Geometry_MatchesVenatorRatioAndFitsCollision()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var visible = view.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            var bounds = visible[0].bounds;
            foreach (var renderer in visible.Skip(1)) bounds.Encapsulate(renderer.bounds);
            Assert.That(bounds.size.z, Is.EqualTo(119.383362f * 250f / 1137f).Within(.01f));
            foreach (var collider in view.GetComponents<BoxCollider>())
                foreach (var renderer in visible)
                {
                    var collisionBounds = new Bounds(collider.center, collider.size + Vector3.one * .001f);
                    var filter = renderer.GetComponents<MeshFilter>().Single();
                    foreach (var vertex in filter.sharedMesh.vertices)
                        Assert.That(collisionBounds.Contains(view.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex))), Is.True, renderer.name);
                }
        }

        [Test]
        public void PowerToWeapons_UsesExistingSelfActivatedAbility()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(ShipAbilityId.TIEAvengerPowerToWeapons);
            Assert.That(definition.DisplayName, Is.EqualTo("Power to Weapons"));
            Assert.That(definition.RequiresEnemyTarget, Is.False);
            Assert.That(definition.Duration, Is.EqualTo(10));
            Assert.That(definition.RecoveryDelay, Is.EqualTo(60));
        }

        [Test]
        public void SavedRegistrations_ResolveOwnDataIconsPlacementAndWreck()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset").GetShipDataPath(ShipType.Tartan), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (string path in new[] { VIEW, DATA })
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/TartanIcon.png");
            var faction = Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue");
            Assert.That(faction.FindPropertyRelative("<Name>k__BackingField").stringValue, Is.EqualTo("Tartan-class Patrol Cruiser"));
            Assert.That(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(AssetDatabase.GUIDToAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue").FindPropertyRelative("m_AssetGUID").stringValue), Is.EqualTo("Assets/Prefabs/Ui/Reinforcement/TartanReinforcementView.prefab"));
            var wreck = new SerializedObject(AssetDatabase.LoadAssetAtPath<ShipData>(DATA).Wreck);
            Assert.That(AssetDatabase.GUIDToAssetPath(wreck.FindProperty("prefab.m_AssetGUID").stringValue), Is.EqualTo("Assets/Prefabs/Models/Wrecks/TartanWreckView.prefab"));
        }

        [TestCase("Assets/Prefabs/Models/Ships/Empire/Models/Tartan.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/TartanWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/TartanReinforcementView.prefab")]
        public void SavedPrefabs_HaveCompleteScriptsGeometryAndReferences(string path)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Vector3 scale = view.transform.localScale;
            Assert.That(scale.x, Is.GreaterThan(0f).And.LessThan(float.PositiveInfinity));
            Assert.That(scale, Is.EqualTo(Vector3.one * scale.x));
            if (path != VIEW && !path.Contains("Wrecks/"))
                Assert.That(scale, Is.EqualTo(Vector3.one));
            foreach (Transform transform in view.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
            foreach (MeshRenderer renderer in view.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
            {
                Assert.That(renderer.GetComponents<MeshFilter>().Single().sharedMesh, Is.Not.Null, renderer.name);
                Assert.That(renderer.sharedMaterials.All(m => m != null), Is.True, renderer.name);
            }
            foreach (MonoBehaviour component in view.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        Assert.That(property.objectReferenceEntityIdValue, Is.EqualTo(EntityId.None), component.name + "/" + property.propertyPath);
            }
        }

        private static SerializedProperty Entry(string path, string field)
        {
            var array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
            return Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex)
                .Single(p => p.FindPropertyRelative("key").intValue == (int)ShipType.Tartan).FindPropertyRelative("value");
        }

    }
}
