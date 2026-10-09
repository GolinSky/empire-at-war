using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceSharedMountTests
    {
        private const string TEST_FOLDER = "Assets/Scripts/Tests/Editor/Balance/TemporarySharedMountAssets";
        private const string SOURCE_PATH = TEST_FOLDER + "/Mount.prefab";
        private const string OWNER_PATH = TEST_FOLDER + "/Owner.prefab";
        private const string DATA_PATH = TEST_FOLDER + "/Data.asset";
        private byte[] _previousRestore;

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(BalanceApplyService.RESTORE_PATH)) _previousRestore = File.ReadAllBytes(BalanceApplyService.RESTORE_PATH);
            AssetDatabase.CreateFolder("Assets/Scripts/Tests/Editor/Balance", "TemporarySharedMountAssets");
            GameObject source = new GameObject("Mount"); WeaponHardPoint sourceMount = source.AddComponent<WeaponHardPoint>();
            using (SerializedObject serialized = new SerializedObject(sourceMount))
            {
                serialized.FindProperty(BalanceRegistration.Auto("WeaponType")).intValue = Convert.ToInt32(Enum.GetValues(typeof(EmpireAtWar.Components.AttackComponent.WeaponType)).GetValue(0));
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObject owner = new GameObject("Owner");
            try
            {
                GameObject sourceAsset = PrefabUtility.SaveAsPrefabAsset(source, SOURCE_PATH);
                GameObject nested = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset, owner.transform);
                WeaponHardPoint mount = BalancePrefabBindings.Components(nested).OfType<WeaponHardPoint>().Single();
                WeaponComponent weapons = owner.AddComponent<WeaponComponent>();
                using (SerializedObject serialized = new SerializedObject(weapons))
                {
                    SerializedProperty list = serialized.FindProperty("hardPoints"); list.arraySize = 1;
                    list.GetArrayElementAtIndex(0).objectReferenceValue = mount;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(owner, OWNER_PATH);
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ShipData>(), DATA_PATH);
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); UnityEngine.Object.DestroyImmediate(source); }
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
            BalanceUnit unit = new BalanceUnit { Id = "Test/1", Name = "Owner", Faction = "Test", Kind = BalanceUnitKind.Ship,
                Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OWNER_PATH), Data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH) };
            registry.Units.Add(unit);
            BalancePrefabBindings.Register(registry, unit);
            BalanceSharedMountAdapter.Register(registry);
            return registry;
        }

        [Test]
        public void ExplicitSharedSourceApply_UpdatesInheritingConsumerWithoutCreatingOverride()
        {
            BalanceRegistration registry = Inventory();
            BalanceField shared = registry.Fields.Values.Single(field => field.SharedMountSource && field.Stat == "mainBattery");
            Assert.That(shared.Users.Count, Is.EqualTo(1));
            byte[] ownerBytes = File.ReadAllBytes(OWNER_PATH);
            BalanceDraft draft = new BalanceDraft(); draft.Set(shared.Snapshot(), "true");
            BalanceField local = registry.Fields.Values.Single(field => !field.SharedMountSource && field.Stat == "mainBattery");
            Assert.That(local.DraftValue(draft), Is.EqualTo("true"));
            // Full presets also contain effective owning values; they must not create redundant overrides.
            draft.Set(local.Snapshot(), "true");
            BalancePreset preset = ScriptableObject.CreateInstance<BalancePreset>();
            AssetDatabase.CreateAsset(preset, TEST_FOLDER + "/Preset.asset");
            BalancePresetService.Save(preset, registry, draft, BalancePresetScope.FullRegisteredSet);
            BalanceApplyService.Apply(draft, Inventory);
            BalanceRegistration saved = Inventory();
            Assert.That(saved.Fields[shared.Key].Read(), Is.EqualTo("true"));
            Assert.That(saved.Fields.Values.Single(field => !field.SharedMountSource && field.Stat == "mainBattery").Read(), Is.EqualTo("true"));
            Assert.That(File.ReadAllBytes(OWNER_PATH), Is.EqualTo(ownerBytes));
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH).WeaponLoadout.Sum(entry => entry.Count), Is.EqualTo(1));
            BalancePresetService.Load(preset, saved, draft);
            Assert.That(draft.Changes, Is.Empty);
            BalanceApplyService.RestorePrevious();
            Assert.That(Inventory().Fields[shared.Key].Read(), Is.EqualTo("false"));
        }

        [Test]
        public void SharedSourceDraft_PreviewsWeaponCompositionWithoutChangingAssets()
        {
            BalanceRegistration registry = Inventory();
            BalanceField shared = registry.Fields.Values.Single(field => field.SharedMountSource && field.Stat == "WeaponType");
            int next = Enum.GetValues(shared.EnumType).Cast<object>().Select(Convert.ToInt32).First(id => id.ToString() != shared.Read());
            BalanceDraft draft = new BalanceDraft(); draft.Set(shared.Snapshot(), next.ToString());
            string preview = BalanceDerivedData.Preview(registry.Units.Single(), registry, draft);
            Assert.That(preview, Is.EqualTo(Enum.GetName(shared.EnumType, next) + " × 1"));
            Assert.That(shared.Read(), Is.Not.EqualTo(next.ToString()));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void OwningOverridePreset_ReloadsAfterApply(bool fullScope, bool legacySchema)
        {
            BalanceRegistration registry = Inventory();
            BalanceField local = registry.Fields.Values.Single(field => !field.SharedMountSource && field.Stat == "mainBattery");
            BalanceDraft draft = new BalanceDraft(); draft.Set(local.Snapshot(), "true");
            BalancePreset preset = ScriptableObject.CreateInstance<BalancePreset>();
            try
            {
                BalancePresetService.Save(preset, registry, draft, fullScope ? BalancePresetScope.FullRegisteredSet : BalancePresetScope.ChangedFieldsOnly);
                if (legacySchema) preset.SetValues(fullScope, preset.Values.Select(entry =>
                    new BalanceSnapshot(entry.Key, registry.Fields[entry.Key].Schema, entry.Value)).ToList());
                BalanceApplyService.Apply(draft, Inventory);
                BalanceRegistration saved = Inventory();
                Assert.That(saved.Fields[local.Key].InheritsSource, Is.False);
                BalancePresetService.Load(preset, saved, draft);
                Assert.That(draft.Changes, Is.Empty);
                BalanceApplyService.RestorePrevious();
                BalancePresetService.Load(preset, Inventory(), draft);
                Assert.That(draft.Changes.Single().Key, Is.EqualTo(local.Key));
                Assert.That(BalanceValidation.Preflight(Inventory(), draft), Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(preset); }
        }

        [Test]
        public void SharedSourceYaw_RejectsInvertedArcAndAcceptsExistingWideLimits()
        {
            BalanceRegistration registry = Inventory();
            BalanceField min = registry.Fields.Values.Single(field => field.SharedMountSource && field.Stat == "MinYaw");
            BalanceField max = registry.Fields.Values.Single(field => field.SharedMountSource && field.Stat == "MaxYaw");
            BalanceDraft draft = new BalanceDraft(); draft.Set(min.Snapshot(), "-360"); draft.Set(max.Snapshot(), "360");
            Assert.That(BalanceValidation.Preflight(registry, draft), Is.Empty);
            draft.Set(min.Snapshot(), "400");
            Assert.That(BalanceValidation.Preflight(registry, draft).Any(error => error.Contains("minimum yaw")), Is.True);
        }
    }
}
