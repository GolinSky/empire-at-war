using EmpireAtWar.Entities.FocusFireCounter;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class FocusFireCounterUiTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/FocusFireCounter/FocusFireCounterUi.prefab";

        [Test]
        public void Prefab_BindsEveryReference_NeverBlocksInput_AndResolvesByItsTypeName()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Assert.That(prefab, Is.Not.Null, PREFAB_PATH);
            FocusFireCounterUi ui = prefab.GetComponent<FocusFireCounterUi>();
            Assert.That(ui, Is.Not.Null);
            using var bindings = new SerializedObject(ui);
            foreach (string field in new[] { "canvasGroup", "screenRect", "anchor", "shipsLabel", "squadronsLabel" })
                Assert.That(bindings.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            CanvasGroup group = prefab.GetComponent<CanvasGroup>();
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.interactable, Is.False);
            foreach (TMP_Text label in prefab.GetComponentsInChildren<TMP_Text>(true))
                Assert.That(label.raycastTarget, Is.False, label.name);

            AddressableAssetEntry entry = AddressableAssetSettingsDefaultObject.Settings
                .FindAssetEntry(AssetDatabase.AssetPathToGUID(PREFAB_PATH));
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.address, Is.EqualTo(nameof(FocusFireCounterUi)));
            Assert.That(entry.parentGroup.Name, Is.EqualTo("Ui"));
        }

        [Test]
        public void ShowCounts_HidesUncappedPools_AndMarksAFullPool()
        {
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH));
            try
            {
                FocusFireCounterUi ui = instance.GetComponent<FocusFireCounterUi>();
                using var bindings = new SerializedObject(ui);
                var anchor = (RectTransform)bindings.FindProperty("anchor").objectReferenceValue;
                var ships = (TMP_Text)bindings.FindProperty("shipsLabel").objectReferenceValue;
                var squadrons = (TMP_Text)bindings.FindProperty("squadronsLabel").objectReferenceValue;
                Color open = bindings.FindProperty("openColor").colorValue;
                Color full = bindings.FindProperty("fullColor").colorValue;
                ui.Initialize();
                Assert.That(anchor.gameObject.activeSelf, Is.False);

                ui.ShowCounts(Vector2.zero, 3, 3, 1, 0);

                Assert.That(anchor.gameObject.activeSelf, Is.True);
                Assert.That(ships.text, Does.Contain("3/3"));
                Assert.That(ships.color, Is.EqualTo(full));
                Assert.That(squadrons.gameObject.activeSelf, Is.False, "No squadron cap → no squadron count.");

                ui.ShowCounts(Vector2.zero, 1, 3, 2, 4);
                Assert.That(ships.color, Is.EqualTo(open));
                Assert.That(squadrons.gameObject.activeSelf, Is.True);
                Assert.That(squadrons.text, Does.Contain("2/4"));

                ui.HideCounts();
                Assert.That(anchor.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
