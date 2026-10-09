using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalancePrefabTests
    {
        private const string TEST_FOLDER = "Assets/Scripts/Tests/Editor/Balance/TemporaryPrefabAssets";
        private const string PREFAB_PATH = TEST_FOLDER + "/TestShip.prefab";
        private const string DATA_PATH = TEST_FOLDER + "/TestData.asset";
        private byte[] _previousRestore;

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(BalanceApplyService.RESTORE_PATH)) _previousRestore = File.ReadAllBytes(BalanceApplyService.RESTORE_PATH);
            AssetDatabase.CreateFolder("Assets/Scripts/Tests/Editor/Balance", "TemporaryPrefabAssets");
            Assert.That(AssetDatabase.CopyAsset("Assets/Prefabs/Models/Ships/ArquitensShipView.prefab", PREFAB_PATH), Is.True);
            ShipData data = ScriptableObject.CreateInstance<ShipData>();
            AssetDatabase.CreateAsset(data, DATA_PATH); AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TEST_FOLDER);
            if (_previousRestore != null) File.WriteAllBytes(BalanceApplyService.RESTORE_PATH, _previousRestore);
            else if (File.Exists(BalanceApplyService.RESTORE_PATH)) File.Delete(BalanceApplyService.RESTORE_PATH);
            AssetDatabase.SaveAssets();
        }

        private static BalanceRegistration Inventory()
        {
            BalanceRegistration registry = new BalanceRegistration();
            BalanceUnit unit = new BalanceUnit
            {
                Id = "test/ship", Name = "Test ship", Kind = BalanceUnitKind.Ship, Faction = "Test",
                Data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH), Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH)
            };
            registry.Units.Add(unit);
            BalancePrefabBindings.Register(registry, unit);
            return registry;
        }

        [Test]
        public void NestedMountApply_WritesOwningPrefabAndPreservesSharedSource()
        {
            BalanceRegistration registry = Inventory();
            BalanceField field = registry.Fields.Values.First(candidate => candidate.Stat == "mainBattery" && PrefabUtility.IsPartOfPrefabInstance(candidate.Target));
            UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(field.Target);
            string sourcePath = AssetDatabase.GetAssetPath(source);
            byte[] sourceBytes = File.ReadAllBytes(sourcePath);
            int objectCount = BalancePrefabBindings.Components(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH)).Count();
            BalanceDraft draft = new BalanceDraft();
            string before = field.Read(); draft.Set(field.Snapshot(), before == "true" ? "false" : "true");
            BalanceApplyService.Apply(draft, Inventory);
            Assert.That(Inventory().Fields[field.Key].Read(), Is.EqualTo(before == "true" ? "false" : "true"));
            Assert.That(File.ReadAllBytes(sourcePath), Is.EqualTo(sourceBytes));
            Assert.That(BalancePrefabBindings.Components(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH)).Count(), Is.EqualTo(objectCount));
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH).WeaponLoadout.Sum(entry => entry.Count), Is.GreaterThan(0));
            BalanceApplyService.RestorePrevious();
            Assert.That(Inventory().Fields[field.Key].Read(), Is.EqualTo(before));
        }

        [Test]
        public void InvalidMountYaw_BlocksPrefabMutation()
        {
            BalanceRegistration registry = Inventory();
            BalanceField field = registry.Fields.Values.First(candidate => candidate.Stat == "MinYaw");
            BalanceDraft draft = new BalanceDraft(); draft.Set(field.Snapshot(), "180");
            BalanceField max = registry.Fields.Values.Single(candidate => candidate.Target == field.Target && candidate.Stat == "MaxYaw");
            draft.Set(max.Snapshot(), "-180");
            Assert.That(BalanceValidation.Preflight(registry, draft).Any(error => error.Contains("minimum yaw")), Is.True);
            Assert.Throws<InvalidOperationException>(() => BalanceApplyService.Apply(draft, Inventory));
        }

        [Test]
        public void CanonicalCatalogEntryIdentity_SurvivesArrayReordering()
        {
            WeaponsData data = ScriptableObject.CreateInstance<WeaponsData>();
            string path = TEST_FOLDER + "/Weapons.asset"; AssetDatabase.CreateAsset(data, path);
            using (SerializedObject serialized = new SerializedObject(data))
            {
                SerializedProperty weapons = serialized.FindProperty("weapons"); weapons.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    weapons.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue = i;
                    weapons.GetArrayElementAtIndex(i).FindPropertyRelative("damage").floatValue = 10 + i;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Func<BalanceRegistration> scan = () =>
            {
                BalanceRegistration result = new BalanceRegistration();
                using (SerializedObject serialized = new SerializedObject(data))
                    foreach (SerializedProperty profile in BalanceRegistration.Elements(serialized.FindProperty("weapons")))
                        result.Add(data, "weapon/" + profile.FindPropertyRelative("weaponType").intValue + "/damage", profile.propertyPath + ".damage",
                            BalanceFieldGroup.Weapons, "Test", BalanceFieldOwner.SharedProfile, true, Array.Empty<BalanceUnit>());
                return result;
            };
            string key = scan().Fields.Values.Single(field => field.Read() == "10").Key;
            using (SerializedObject serialized = new SerializedObject(data))
            { serialized.FindProperty("weapons").MoveArrayElement(0, 1); serialized.ApplyModifiedPropertiesWithoutUndo(); }
            Assert.That(scan().Fields[key].Read(), Is.EqualTo("10"));
            AssetDatabase.SaveAssets();
        }
    }
}
