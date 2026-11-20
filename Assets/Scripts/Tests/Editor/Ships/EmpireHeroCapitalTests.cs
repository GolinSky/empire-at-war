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
    /// <summary>Chimaera and Devastator: ISD II-based Empire hero capitals.</summary>
    [Category("Ships")]
    public sealed class EmpireHeroCapitalTests
    {
        private const string ISDII_VIEW = "Assets/Prefabs/Models/Ships/Empire/View/ISDIIShipView.prefab";
        private const string CHIMAERA_MATERIALS = "Assets/Art/Materials/Models/EmpireShips/Chimaera";

        private static string View(string name) => $"Assets/Prefabs/Models/Ships/Empire/View/{name}ShipView.prefab";
        private static string Data(string name) => $"Assets/Settings/Data/Ship/Empire/{name}ShipData.asset";
        private static string Reinforcement(string name) => $"Assets/Prefabs/Ui/Reinforcement/{name}ReinforcementView.prefab";

        [TestCase(ShipType.Chimaera, "Chimaera")]
        [TestCase(ShipType.Devastator, "Devastator")]
        public void SavedHero_IsRegisteredAsUniqueEmpireHero(ShipType type, string name)
        {
            ShipsData ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(type), Is.EqualTo(AssetDatabase.AssetPathToGUID(Data(name))));
            foreach (string path in new[] { View(name), Data(name), Reinforcement(name) })
            {
                var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.That(entry.address, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)), path);
            }

            SerializedProperty faction = Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset", "ships.keyValue", type);
            Assert.That(faction.FindPropertyRelative("isHero").boolValue, Is.True);
            Assert.That(faction.FindPropertyRelative("<MaxCount>k__BackingField").intValue, Is.EqualTo(1));
            Assert.That(faction.FindPropertyRelative("matchups").objectReferenceValue, Is.Not.Null);
            Object icon = faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue;
            Assert.That(icon, Is.Not.Null);
            Assert.That(faction.FindPropertyRelative("iconKey").stringValue,
                Does.StartWith(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(icon))));
            Assert.That(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset", "shipIconWrapper.keyValue", type)
                .objectReferenceValue, Is.EqualTo(icon));
            Assert.That(AssetDatabase.LoadAssetAtPath<TooltipIconData>("Assets/Settings/Data/Tooltip/TooltipIconData.asset")
                .Resolve(faction.FindPropertyRelative("iconKey").stringValue), Is.EqualTo(icon));
            string spawn = AssetDatabase.GUIDToAssetPath(Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset",
                "spawnShipWrapper.keyValue", type).FindPropertyRelative("m_AssetGUID").stringValue);
            Assert.That(spawn, Is.EqualTo(Reinforcement(name)));
            Assert.That(Entry("Assets/Settings/Data/Models/Audio/ShipSfxData.asset", "ships.keyValue", type), Is.Not.Null);
            Assert.That(Entry("Assets/Settings/Data/Models/Audio/ShipSfxData.asset", "shipVoices.keyValue", type), Is.Not.Null);
        }

        [TestCase("Chimaera")]
        [TestCase("Devastator")]
        public void SavedView_IsIsdIIVariantWithEveryHardPointRegistered(string name)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(View(name));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(view),
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(ISDII_VIEW)));
            HardPoint[] hardPoints = view.GetComponentsInChildren<HardPoint>(true);
            WeaponHardPoint[] weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            HealthComponent health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            Assert.That(hardPoints.Select(h => h.Id).Distinct().Count(), Is.EqualTo(hardPoints.Length));
            Assert.That(health.ShipUnits, Is.EquivalentTo(hardPoints));
            Assert.That(References(view, "WeaponComponent", "hardPoints"), Is.EquivalentTo(weapons));
            Assert.That(References(view, "EntityBindings", "<Injectables>k__BackingField"), Is.SupersetOf(weapons));
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(Data(name)).Abilities,
                Is.EquivalentTo(AssetDatabase.LoadAssetAtPath<ShipData>("Assets/Settings/Data/Ship/Empire/ISDIIShipData.asset").Abilities));
        }

        [Test]
        public void Chimaera_AddsTwoDualMediumTurbolasersToTheIsdIIBatteries()
        {
            WeaponHardPoint[] isd = Weapons(ISDII_VIEW);
            WeaponHardPoint[] chimaera = Weapons(View("Chimaera"));
            Assert.That(Count(chimaera, WeaponType.DualMediumTurboLaser), Is.EqualTo(Count(isd, WeaponType.DualMediumTurboLaser) + 2));
            Assert.That(chimaera.Length, Is.EqualTo(isd.Length + 2));
            Assert.That(MainBatteries(chimaera).Select(w => w.WeaponType), Is.EquivalentTo(MainBatteries(isd).Select(w => w.WeaponType)));
        }

        [Test]
        public void Devastator_ReplacesOctuplesWithMirroredHeavyDualTurbolasersAndIonCannons()
        {
            WeaponHardPoint[] isd = Weapons(ISDII_VIEW);
            WeaponHardPoint[] devastator = Weapons(View("Devastator"));
            WeaponHardPoint[] mains = MainBatteries(devastator);
            Assert.That(mains.Length, Is.EqualTo(MainBatteries(isd).Length));
            Assert.That(Count(mains, WeaponType.ISDIIOctupleTurbolaser), Is.Zero);
            Assert.That(Count(mains, WeaponType.HeavyDualTurbolaser), Is.EqualTo(6));
            WeaponHardPoint[] ions = mains.Where(w => w.WeaponType == WeaponType.HeavyDualIonCannon).ToArray();
            Assert.That(ions.Length, Is.EqualTo(2));
            Assert.That(ions[0].transform.localPosition.x, Is.EqualTo(-ions[1].transform.localPosition.x).Within(0.01f));
            Assert.That(Count(devastator, WeaponType.MediumIonCannon), Is.EqualTo(Count(isd, WeaponType.MediumIonCannon) + 2));
        }

        [Test]
        public void Chimaera_UsesWeatheredMaterialsAndRendersItsEmblem()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(View("Chimaera"));
            string[] weathered = AssetDatabase.FindAssets("t:Material", new[] { CHIMAERA_MATERIALS })
                .Select(g => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            foreach (MeshRenderer renderer in view.GetComponentsInChildren<MeshRenderer>(true))
            foreach (Material material in renderer.sharedMaterials)
                Assert.That(weathered, Has.No.Member(material.name.Replace("ISDIIReplacement_ISDII_", "Chimaera_")),
                    $"{renderer.name} still uses {material.name}");
            MeshRenderer emblem = view.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == "ChimaeraEmblem");
            Assert.That(References(view, "FogVisibilityComponent", "renderers"), Has.Member(emblem));
            Assert.That(References(view, "UnitRenderStateComponent", "meshRenderers"), Has.Member(emblem));
            Assert.That(AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<ShipData>(Data("Chimaera")).Wreck),
                Is.EqualTo("Assets/Settings/Data/Models/Wrecks/Empire/ChimaeraWreckData.asset"));
        }

        [TestCase("Assets/Prefabs/Models/Ships/Empire/View/ChimaeraShipView.prefab")]
        [TestCase("Assets/Prefabs/Models/Ships/Empire/View/DevastatorShipView.prefab")]
        [TestCase("Assets/Prefabs/Models/Wrecks/ChimaeraWreckView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/ChimaeraReinforcementView.prefab")]
        [TestCase("Assets/Prefabs/Ui/Reinforcement/DevastatorReinforcementView.prefab")]
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

        private static WeaponHardPoint[] MainBatteries(WeaponHardPoint[] weapons) =>
            weapons.Where(w => new SerializedObject(w).FindProperty("mainBattery").boolValue).ToArray();

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
