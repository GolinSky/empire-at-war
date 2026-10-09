using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceCompareTests
    {
        private const string TEST_FOLDER = "Assets/Scripts/Tests/Editor/Balance/TemporaryCompareAssets";

        [UnityTest]
        public IEnumerator Picker_FiltersSelectsAndAddsEachUnitOnce()
        {
            BalanceRegistration registry = new BalanceRegistration();
            registry.Units.Add(new BalanceUnit { Id = "empire/1", Name = "Alpha", Faction = "Empire", Kind = BalanceUnitKind.Ship, Class = "Frigate" });
            registry.Units.Add(new BalanceUnit { Id = "empire/2", Name = "Beta", Faction = "Empire", Kind = BalanceUnitKind.Ship, Class = "Frigate" });
            registry.Units.Add(new BalanceUnit { Id = "republic/1", Name = "Gamma", Faction = "Republic", Kind = BalanceUnitKind.Ship, Class = "Frigate" });
            BalanceWindowState state = new BalanceWindowState();
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            int refreshes = 0;
            try
            {
                host.position = new Rect(100, 100, 960, 550);
                host.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
                BalanceCompareView.Build(host.rootVisualElement, registry, new BalanceEditorController(state, () => refreshes++), false);
                host.Show();
                yield return null; yield return null;
                VisualElement picker = host.rootVisualElement.Q("compare-picker");
                ListView list = picker.Q<ListView>();
                Button addSelected = picker.Q<Button>("compare-add-selected");
                void Click(string name)
                {
                    Button button = picker.Q<Button>(name);
                    if (button.enabledInHierarchy)
                        typeof(Clickable).GetMethod("SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(button.clickable, new object[] { null, 0 });
                }
                Assert.That(addSelected.enabledSelf, Is.False);
                picker.Q<DropdownField>("compare-faction").value = "Empire";
                Assert.That(list.itemsSource.Cast<BalanceUnit>().Select(unit => unit.Id), Is.EquivalentTo(new[] { "empire/1", "empire/2" }));
                Click("compare-select-all");
                Assert.That(addSelected.text, Is.EqualTo("Add selected (2)"));
                picker.Q<TextField>("compare-search").value = "Alpha";
                Click("compare-unselect-all");
                Assert.That(addSelected.text, Is.EqualTo("Add selected (1)"));
                Click("compare-select-all");
                Click("compare-add-selected");
                Assert.That(state.Pins, Is.EquivalentTo(new[] { "empire/1", "empire/2" }));
                Assert.That(list.itemsSource.Count, Is.Zero);
                Assert.That(addSelected.enabledSelf, Is.False);
                Click("compare-add-selected");
                Assert.That(state.Pins.Count, Is.EqualTo(2));
                picker.Q<TextField>("compare-search").value = "";
                picker.Q<DropdownField>("compare-faction").value = "All factions";
                yield return null; yield return null;
                VisualElement row = picker.Query<VisualElement>(className: "balance-picker-row").ToList()
                    .Single(element => element.userData is BalanceUnit unit && unit.Id == "republic/1");
                row.Q<Toggle>("picker-select").value = true;
                Assert.That(addSelected.text, Is.EqualTo("Add selected (1)"));
                Click("picker-add");
                Assert.That(state.Pins, Is.EquivalentTo(registry.Units.Select(unit => unit.Id)));
                Assert.That(list.itemsSource.Count, Is.Zero);
                Assert.That(addSelected.text, Is.EqualTo("Add selected (0)"));
                Assert.That(refreshes, Is.EqualTo(2));
                state.Pins.Remove("empire/1");
                picker.Q<TextField>("compare-search").value = "Alpha";
                Assert.That(list.itemsSource.Cast<BalanceUnit>().Single().Id, Is.EqualTo("empire/1"));
            }
            finally { host.Close(); }
        }

        [TestCase("Empire")]
        [TestCase("Republic")]
        public void SameAndCrossFactionCards_EditLocalAndSharedSlidersWithoutAssetWrites(string secondFaction)
        {
            AssetDatabase.CreateFolder("Assets/Scripts/Tests/Editor/Balance", "TemporaryCompareAssets");
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                BalanceRegistration registry = new BalanceRegistration();
                for (int i = 0; i < 2; i++)
                {
                    ShipData data = ScriptableObject.CreateInstance<ShipData>();
                    AssetDatabase.CreateAsset(data, TEST_FOLDER + "/Ship" + i + ".asset");
                    BalanceUnit unit = new BalanceUnit { Id = "test/" + i, Name = "Ship " + i, Kind = BalanceUnitKind.Ship, Class = "Frigate", Faction = i == 0 ? "Empire" : secondFaction, Data = data };
                    registry.Units.Add(unit);
                    unit.Fields["Hull"] = registry.Add(data, "Hull", BalanceRegistration.Auto("Hull"), BalanceFieldGroup.Combat, unit.Name, BalanceFieldOwner.UnitData, false, new[] { unit }).Key;
                    unit.Fields["Speed"] = registry.Add(data, "Speed", BalanceRegistration.Auto("Speed"), BalanceFieldGroup.Movement, unit.Name, BalanceFieldOwner.UnitData, false, new[] { unit }).Key;
                    unit.Fields["HeightTier"] = registry.Add(data, "HeightTier", BalanceRegistration.Auto("HeightTier"), BalanceFieldGroup.Movement, unit.Name, BalanceFieldOwner.UnitData, false, new[] { unit }, typeof(ShipHeightTier)).Key;
                }
                BalanceField shared = registry.Add(registry.Units[0].Data, "Shields", BalanceRegistration.Auto("Shields"), BalanceFieldGroup.Combat, "Shared test profile", BalanceFieldOwner.SharedProfile, true, registry.Units);
                foreach (BalanceUnit unit in registry.Units) unit.Fields["Shields"] = shared.Key;
                AssetDatabase.SaveAssets();
                Dictionary<string, byte[]> before = registry.Units.ToDictionary(unit => AssetDatabase.GetAssetPath(unit.Data), unit => File.ReadAllBytes(AssetDatabase.GetAssetPath(unit.Data)));
                BalanceWindowState state = new BalanceWindowState { Pins = registry.Units.Select(unit => unit.Id).ToList() };
                Action build = () => { host.rootVisualElement.Clear(); BalanceCompareView.Build(host.rootVisualElement, registry, new BalanceEditorController(state, () => { }), false); };
                host.Show(); build();
                var hulls = host.rootVisualElement.Query<VisualElement>(className: "balance-hull-editor").ToList().Select(editor => editor.Q<FloatField>("stat-value")).ToList();
                hulls[0].value = 125;
                Assert.That(state.Draft.Changes.Single().Key, Is.EqualTo(registry.Units[0].Fields["Hull"]));
                Assert.That(hulls[1].value, Is.Not.EqualTo(125));
                host.rootVisualElement.Query<VisualElement>(className: "balance-shield-editor").ToList()[0].Q<Slider>("stat-slider").value = 75;
                build();
                Assert.That(host.rootVisualElement.Query<VisualElement>(className: "balance-shield-editor").ToList().All(editor => editor.Q<FloatField>("stat-value").value == 75), Is.True);
                Assert.That(state.Draft.Changes.Count, Is.EqualTo(2));
                Assert.That(shared.Users.Count, Is.EqualTo(2));
                var speed = host.rootVisualElement.Query<VisualElement>(className: "balance-compare-edit").ToList().Single(row => (string)row.userData == registry.Units[0].Fields["Speed"]);
                speed.Q<FloatField>().value = 42;
                var height = host.rootVisualElement.Query<VisualElement>(className: "balance-compare-edit").ToList().Single(row => (string)row.userData == registry.Units[0].Fields["HeightTier"]);
                height.Q<EnumField>().value = ShipHeightTier.High;
                Assert.That(state.Draft.Changes.Single(change => change.Key == registry.Units[0].Fields["Speed"]).After, Is.EqualTo("42"));
                Assert.That(state.Draft.Changes.Single(change => change.Key == registry.Units[0].Fields["HeightTier"]).After, Is.EqualTo(((int)ShipHeightTier.High).ToString()));
                foreach (var asset in before) Assert.That(File.ReadAllBytes(asset.Key), Is.EqualTo(asset.Value));
            }
            finally { host.Close(); AssetDatabase.DeleteAsset(TEST_FOLDER); AssetDatabase.SaveAssets(); }
        }
    }
}
