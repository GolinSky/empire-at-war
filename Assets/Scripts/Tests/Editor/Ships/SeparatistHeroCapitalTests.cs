using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Invisible Hand: Providence-based Separatist hero capital with one extra heavy dual proton torpedo.</summary>
    [Category("Ships")]
    public sealed class SeparatistHeroCapitalTests
    {
        private const string PROVIDENCE_VIEW = "Assets/Prefabs/Models/Ships/Separatist/View/ProvidenceShipView.prefab";
        private const string PROVIDENCE_DATA = "Assets/Settings/Data/Ship/Separatist/ProvidenceShipData.asset";
        private const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/View/InvisibleHandShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Separatist/InvisibleHandShipData.asset";
        private const string REINFORCEMENT = "Assets/Prefabs/Ui/Reinforcement/InvisibleHandReinforcementView.prefab";

        [Test]
        public void SavedHero_IsRegisteredAsUniqueSeparatistHero()
        {
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(ShipType.InvisibleHand), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (string path in new[] { VIEW, DATA, REINFORCEMENT })
            {
                var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.That(entry.address, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)), path);
            }

            SerializedProperty faction = Entry("Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset", "ships.keyValue",
                ShipType.InvisibleHand);
            Assert.That(faction.FindPropertyRelative("isHero").boolValue, Is.True);
            Assert.That(faction.FindPropertyRelative("<MaxCount>k__BackingField").intValue, Is.EqualTo(1));
            Assert.That(faction.FindPropertyRelative("matchups").objectReferenceValue, Is.Not.Null);
            Object icon = faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue;
            Assert.That(icon, Is.Not.Null);
            Assert.That(faction.FindPropertyRelative("iconKey").stringValue,
                Does.StartWith(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(icon))));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue", ShipType.InvisibleHand)
                .objectReferenceValue, Is.EqualTo(icon));
            Assert.That(AssetDatabase.LoadAssetAtPath<TooltipIconData>("Assets/Settings/Data/Tooltip/TooltipIconData.asset")
                .Resolve(faction.FindPropertyRelative("iconKey").stringValue), Is.EqualTo(icon));
            string spawn = AssetDatabase.GUIDToAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset",
                "spawnShipWrapper.keyValue", ShipType.InvisibleHand).FindPropertyRelative("m_AssetGUID").stringValue);
            Assert.That(spawn, Is.EqualTo(REINFORCEMENT));
            Assert.That(Entry("Assets/Settings/Data/Models/Audio/ShipSfxData.asset", "ships.keyValue", ShipType.InvisibleHand),
                Is.Not.Null);
        }

        [Test]
        public void SavedView_IsProvidenceVariantWithEveryHardPointRegistered()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(view),
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(PROVIDENCE_VIEW)));
            HardPoint[] hardPoints = view.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            Assert.That(hardPoints.Select(h => h.Id).Distinct().Count(), Is.EqualTo(hardPoints.Length));
            Assert.That(health.ShipUnits, Is.EquivalentTo(hardPoints));
            Assert.That(References(view, "WeaponComponent", "hardPoints"), Is.EquivalentTo(weapons));
            Assert.That(References(view, "EntityBindings", "<Injectables>k__BackingField"), Is.SupersetOf(weapons));
        }

        [Test]
        public void InvisibleHand_AddsOneHeavyDualProtonTorpedoToTheProvidence()
        {
            WeaponHardPoint[] providence = Weapons(PROVIDENCE_VIEW);
            WeaponHardPoint[] hero = Weapons(VIEW);
            Assert.That(hero.Length, Is.EqualTo(providence.Length + 1));
            Assert.That(Count(hero, WeaponType.HeavyDualProtonTorpedo),
                Is.EqualTo(Count(providence, WeaponType.HeavyDualProtonTorpedo) + 1));
            foreach (WeaponType type in providence.Select(w => w.WeaponType).Distinct().Where(t => t != WeaponType.HeavyDualProtonTorpedo))
                Assert.That(Count(hero, type), Is.EqualTo(Count(providence, type)), type.ToString());
        }

        [Test]
        public void SavedData_KeepsProvidenceAbilitiesAndIsTougher()
        {
            ShipData providence = AssetDatabase.LoadAssetAtPath<ShipData>(PROVIDENCE_DATA);
            ShipData hero = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(hero.Abilities, Is.EquivalentTo(providence.Abilities));
            Assert.That(hero.ShipClass, Is.EqualTo(providence.ShipClass));
            Assert.That(hero.Hull, Is.GreaterThan(providence.Hull));
            Assert.That(hero.Shields, Is.GreaterThan(providence.Shields));
        }

        [TestCase(VIEW)]
        [TestCase(REINFORCEMENT)]
        public void SavedPrefab_HasNoMissingScriptsOrMaterials(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), Is.Zero, t.name);
            foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
            {
                Assert.That(renderer.GetComponents<MeshFilter>().Single().sharedMesh, Is.Not.Null, renderer.name);
                Assert.That(renderer.sharedMaterials.All(m => m != null), Is.True, renderer.name);
            }
        }

        private static WeaponHardPoint[] Weapons(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<WeaponHardPoint>(true);

        private static int Count(WeaponHardPoint[] weapons, WeaponType type) => weapons.Count(w => w.WeaponType == type);

        private static Object[] References(GameObject view, string component, string property)
        {
            MonoBehaviour owner = view.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == component);
            SerializedProperty list = new SerializedObject(owner).FindProperty(property);
            return Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        }

        private static SerializedProperty Entry(string path, string field, ShipType type)
        {
            SerializedProperty array = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
            return Enumerable.Range(0, array.arraySize).Select(array.GetArrayElementAtIndex)
                .Single(p => p.FindPropertyRelative("key").intValue == (int)type).FindPropertyRelative("value");
        }
    }
}
