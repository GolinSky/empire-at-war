using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalancePersistenceTests
    {
        private const string TEST_FOLDER = "Assets/Scripts/Tests/Editor/Balance/TemporaryBalanceAssets";
        private const string DATA_PATH = TEST_FOLDER + "/TestShip.asset";
        private byte[] _previousRestore;

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(BalanceApplyService.RESTORE_PATH)) _previousRestore = File.ReadAllBytes(BalanceApplyService.RESTORE_PATH);
            AssetDatabase.CreateFolder("Assets/Scripts/Tests/Editor/Balance", "TemporaryBalanceAssets");
            ShipData data = ScriptableObject.CreateInstance<ShipData>();
            using (SerializedObject serialized = new SerializedObject(data))
            {
                serialized.FindProperty(BalanceRegistration.Auto("Hull")).floatValue = 100;
                serialized.FindProperty(BalanceRegistration.Auto("Speed")).floatValue = 7;
                serialized.FindProperty(BalanceRegistration.Auto("HullBottom")).floatValue = -9;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(data, DATA_PATH);
            AssetDatabase.SaveAssets();
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
            registry.Add(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH), "Hull", BalanceRegistration.Auto("Hull"), BalanceFieldGroup.Combat, "Test", BalanceFieldOwner.UnitData, false, Array.Empty<BalanceUnit>());
            return registry;
        }

        [Test]
        public void ApplyRestore_PersistsOnlyAllowedFieldAndPreservesUnrelatedData()
        {
            BalanceField field = Inventory().Fields.Values.Single();
            BalanceDraft draft = new BalanceDraft(); draft.Set(field.Snapshot(), "140");
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH).Hull, Is.EqualTo(100));
            BalanceApplyService.Apply(draft, Inventory);
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH);
            Assert.That(data.Hull, Is.EqualTo(140)); Assert.That(data.Speed, Is.EqualTo(7)); Assert.That(data.HullBottom, Is.EqualTo(-9));
            Assert.That(draft.Changes, Is.Empty);
            BalanceApplyService.RestorePrevious();
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH).Hull, Is.EqualTo(100));
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH).HullBottom, Is.EqualTo(-9));
        }

        [Test]
        public void ExternalEdit_BlocksAllWritesAndRetainsDraft()
        {
            BalanceField field = Inventory().Fields.Values.Single();
            BalanceDraft draft = new BalanceDraft(); draft.Set(field.Snapshot(), "140");
            using (SerializedObject serialized = new SerializedObject(field.Target))
            {
                serialized.FindProperty(field.Path).floatValue = 120;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(field.Target); AssetDatabase.SaveAssets();
            }
            Assert.Throws<InvalidOperationException>(() => BalanceApplyService.Apply(draft, Inventory));
            Assert.That(field.Read(), Is.EqualTo("120")); Assert.That(draft.Changes.Count, Is.EqualTo(1));
        }

        [Test]
        public void FailedReadback_RollsBackWrittenFileAndKeepsDraft()
        {
            BalanceField field = Inventory().Fields.Values.Single();
            BalanceDraft draft = new BalanceDraft(); draft.Set(field.Snapshot(), "140");
            int scans = 0;
            InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => BalanceApplyService.Apply(draft, () =>
            {
                BalanceRegistration registry = Inventory();
                if (++scans > 1) registry.Errors.Add("Injected read-back failure");
                return registry;
            }));
            Assert.That(failure.Message, Does.Contain("Rollback imported and verified"));
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH).Hull, Is.EqualTo(100));
            Assert.That(draft.Changes.Count, Is.EqualTo(1));
        }

        [Test]
        public void PresetRoundTrip_LeavesLiveAssetUntouched()
        {
            BalanceRegistration registry = Inventory(); BalanceField field = registry.Fields.Values.Single();
            BalanceDraft draft = new BalanceDraft(); draft.Set(field.Snapshot(), "140");
            BalancePreset preset = ScriptableObject.CreateInstance<BalancePreset>();
            AssetDatabase.CreateAsset(preset, TEST_FOLDER + "/Preset.asset");
            BalancePresetService.Save(preset, registry, draft, BalancePresetScope.ChangedFieldsOnly);
            draft.Discard();
            AssetDatabase.ImportAsset(TEST_FOLDER + "/Preset.asset", ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            BalancePresetService.Load(AssetDatabase.LoadAssetAtPath<BalancePreset>(TEST_FOLDER + "/Preset.asset"), Inventory(), draft);
            Assert.That(draft.Value(field.Key, "100"), Is.EqualTo("140"));
            Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH).Hull, Is.EqualTo(100));
        }

        [Test]
        public void DirectComparisonControl_StagesSharedValueWithoutTouchingAsset()
        {
            BalanceRegistration registry = Inventory(); BalanceField field = registry.Fields.Values.Single();
            BalanceWindowState state = new BalanceWindowState();
            byte[] before = File.ReadAllBytes(DATA_PATH);
            UnityEngine.UIElements.VisualElement first = BalanceFieldView.Create(field, state, new BalanceDraftUsage(registry, state.Draft), value => state.Draft.Set(field.Snapshot(), value), () => { }, true);
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                host.Show(); host.rootVisualElement.Add(first);
                first.Q<FloatField>().value = 150;
                UnityEngine.UIElements.VisualElement linked = BalanceFieldView.Create(field, state, new BalanceDraftUsage(registry, state.Draft), value => state.Draft.Set(field.Snapshot(), value), () => { }, true);
                host.rootVisualElement.Add(linked);
                Assert.That(linked.Q<FloatField>().value, Is.EqualTo(150));
                Assert.That(state.Draft.Changes.Count, Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(DATA_PATH), Is.EqualTo(before));
                Assert.That(AssetDatabase.LoadAssetAtPath<ShipData>(DATA_PATH).Hull, Is.EqualTo(100));
            }
            finally { host.Close(); }
        }

        [Test]
        public void ManagedReferenceApply_PreservesAliasAndDefinitionTiming()
        {
            string path = TEST_FOLDER + "/Abilities.asset";
            AssetDatabase.CopyAsset("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset", path);
            Func<BalanceRegistration> scan = () =>
            {
                var catalog = AssetDatabase.LoadAssetAtPath<EmpireAtWar.Services.ShipAbilities.ShipAbilityCatalog>(path);
                BalanceRegistration registry = new BalanceRegistration();
                using (SerializedObject serialized = new SerializedObject(catalog))
                {
                    SerializedProperty entry = BalanceRegistration.Keyed(serialized, "definitions.keyValue").Single(row => row.FindPropertyRelative("key").intValue == 10);
                    registry.Add(catalog, "settings/1+10/damage", entry.propertyPath + ".value.settings.damage", BalanceFieldGroup.Abilities, "Shared beams", BalanceFieldOwner.SharedProfile, true, Array.Empty<BalanceUnit>(), alias: "Shared settings — Laser Beam + Proton Beam");
                }
                return registry;
            };
            var original = AssetDatabase.LoadAssetAtPath<EmpireAtWar.Services.ShipAbilities.ShipAbilityCatalog>(path);
            float duration = original.Get(EmpireAtWar.Services.ShipAbilities.ShipAbilityId.ProtonBeam).Duration;
            BalanceField field = scan().Fields.Values.Single(); BalanceDraft draft = new BalanceDraft(); draft.Set(field.Snapshot(), "1234");
            BalanceApplyService.Apply(draft, scan);
            var saved = AssetDatabase.LoadAssetAtPath<EmpireAtWar.Services.ShipAbilities.ShipAbilityCatalog>(path);
            Assert.That(ReferenceEquals(saved.Get(EmpireAtWar.Services.ShipAbilities.ShipAbilityId.ProtonBeam).Settings, saved.Get(EmpireAtWar.Services.ShipAbilities.ShipAbilityId.LaserBeam).Settings), Is.True);
            Assert.That(saved.Get(EmpireAtWar.Services.ShipAbilities.ShipAbilityId.ProtonBeam).Duration, Is.EqualTo(duration));
            Assert.That(scan().Fields.Values.Single().Read(), Is.EqualTo("1234"));
        }
    }
}
