using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Empire Xyston: superlaser glass cannon built from Tools/Blender/Xyston.</summary>
    [Category("Ships")]
    public sealed class XystonShipTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/XystonShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Empire/XystonShipData.asset";
        private const string CATALOG = "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset";

        [Test]
        public void SavedShip_HasTargetableHardpointsTractorAndNoHangar()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.Abilities, Is.EquivalentTo(new[] { ShipAbilityId.XystonSuperlaser, ShipAbilityId.TractorBeam }));
            Assert.That(data.HangarBays, Is.Empty);
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(view.GetComponentsInChildren<MonoBehaviour>(true).Any(c => c.GetType().Name == "HangarComponent"), Is.False);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            HardPoint[] points = view.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            Assert.That(health.ShipUnits.Select(p => p.Id).OrderBy(id => id), Is.EqualTo(Enumerable.Range(0, points.Length)));
            foreach (HardPointType required in new[] { HardPointType.ShieldGenerator, HardPointType.Engines, HardPointType.TractorBeam })
                Assert.That(health.ShipUnits.Any(p => p.HardPointType == required), Is.True, required.ToString());
            Assert.That(health.ShipUnits.Any(p => p.HardPointType == HardPointType.Hangar), Is.False);
            Assert.That(weapons.All(health.ShipUnits.Contains), Is.True);
            Assert.That(data.WeaponLoadout.Sum(entry => entry.Count), Is.EqualTo(weapons.Length));
            foreach (var group in weapons.GroupBy(w => w.WeaponType))
                Assert.That(data.WeaponLoadout.Single(entry => entry.WeaponType == group.Key).Count, Is.EqualTo(group.Count()), group.Key.ToString());
        }

        [Test]
        public void SavedShip_SuperlaserFiresFromTheSpinalCannonMuzzle()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Transform muzzle = view.GetComponentsInChildren<ShipEntity>(true).Single().CompositeBeamMuzzle;
            Assert.That(muzzle, Is.Not.Null);
            Assert.That(muzzle.root, Is.EqualTo(view.transform));
            Transform cannon = view.GetComponentsInChildren<Transform>(true).Single(t => t.name == "XystonCannon");
            Assert.That(muzzle.IsChildOf(cannon), Is.True);
            // Bow is +Z: the barrel runs forward from its mount to the fire point.
            Assert.That(view.transform.InverseTransformPoint(muzzle.position).z,
                Is.GreaterThan(view.transform.InverseTransformPoint(cannon.position).z));
        }

        [Test]
        public void Superlaser_ScalesByTargetClassAndXystonIsMoreFragileThanTheAllegiance()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(CATALOG);
            ShipAbilityDefinition superlaser = catalog.Get(ShipAbilityId.XystonSuperlaser);
            var settings = (SuperlaserSettings)superlaser.Settings;
            Assert.That(superlaser.RequiresEnemyTarget, Is.True);
            Assert.That(superlaser.Range, Is.GreaterThan(0f));
            Assert.That(superlaser.AutoPower, Is.EqualTo(AutoPowerRole.Target));
            Assert.That(settings.Damage, Is.GreaterThan(0f));

            // The superlaser's own per-class scale is the whole matchup: its matrix row is neutral against every target.
            Assert.That(settings.DamageType, Is.EqualTo(DamageType.Superlaser));
            var matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:DamageMatrixData").Single()));
            Assert.That(matrix.GetShipShieldMultiplier(settings.DamageType), Is.EqualTo(1f));
            Assert.That(matrix.IsShieldPiercing(settings.DamageType), Is.False);
            foreach (ShipClass target in new[] { ShipClass.Frigate, ShipClass.Cruiser, ShipClass.HeavyCruiser, ShipClass.Capital,
                         ShipClass.BattleCruiser, ShipClass.Dreadnought, ShipClass.Structure })
                Assert.That(matrix.GetDamageMultiplier(settings.DamageType, target), Is.EqualTo(1f), target.ToString());

            // Stations take the most, then structures and the largest ships, down to frigates.
            float[] byTarget = { settings.VsStation, settings.VsStructure, settings.VsDreadnought, settings.VsBattleCruiser,
                settings.VsCapital, settings.VsHeavyCruiser, settings.VsCruiser, settings.VsOther };
            Assert.That(byTarget, Is.Ordered.Descending);
            Assert.That(settings.VsOther, Is.GreaterThan(0f));

            // The Xyston outlasts an ISD II but stays more fragile than the Allegiance battlecruiser.
            ShipData xyston = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            foreach (string line in new[] { "AllegianceShipData" })
            {
                ShipData other = AssetDatabase.LoadAssetAtPath<ShipData>($"Assets/Settings/Data/Ship/Empire/{line}.asset");
                Assert.That(xyston.Hull + xyston.Shields, Is.LessThan(other.Hull + other.Shields), line);
            }
        }

        [Test]
        public void SavedRegistrations_ResolveOwnAssetsAndIcons()
        {
            Assert.That((int)ShipType.Xyston, Is.EqualTo(220));
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(ShipType.Xyston), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (string path in new[] { VIEW, DATA })
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address,
                    Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/XystonIcon.png");
            Assert.That(icon, Is.Not.Null);
            Assert.That(Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue")
                .FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue").objectReferenceValue, Is.EqualTo(icon));
            Assert.That(AssetDatabase.GUIDToAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset", "spawnShipWrapper.keyValue")
                    .FindPropertyRelative("m_AssetGUID").stringValue),
                Is.EqualTo("Assets/Prefabs/Ui/Reinforcement/XystonReinforcementView.prefab"));
        }

        [TestCase("Assets/Prefabs/Models/Ships/Empire/Models/Xyston.prefab")]
        [TestCase(VIEW)]
        [TestCase("Assets/Prefabs/Models/Wrecks/XystonWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/XystonReinforcementView.prefab")]
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

        private static SerializedProperty Entry(string path, string field)
        {
            SerializedProperty array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
            return Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex)
                .Single(p => p.FindPropertyRelative("key").intValue == (int)ShipType.Xyston).FindPropertyRelative("value");
        }
    }
}
