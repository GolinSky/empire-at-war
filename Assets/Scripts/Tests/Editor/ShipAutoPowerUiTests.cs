using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Models.ShipUi;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class ShipAutoPowerUiTests
    {
        [TestCase("Assets/Prefabs/Ui/Ship/ShipUi.prefab")]
        [TestCase("Assets/Prefabs/Ui/Ship/ShipGroupUi.prefab")]
        public void AbilityBarsHaveBoundAutoPowerControls(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            ShipAbilityBarUi[] bars = prefab.GetComponentsInChildren<ShipAbilityBarUi>(true);
            Assert.That(bars, Is.Not.Empty);
            foreach (ShipAbilityBarUi bar in bars)
            {
                Object control = new SerializedObject(bar).FindProperty("autoPowerButton").objectReferenceValue;
                Assert.That(control, Is.InstanceOf<ShipAutoPowerButtonUi>());
                SerializedObject serializedControl = new SerializedObject(control);
                foreach (string field in new[] { "button", "label", "activeHighlight", "tooltipTrigger" })
                    Assert.That(serializedControl.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                Button button = (Button)serializedControl.FindProperty("button").objectReferenceValue;
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
                Assert.That(button.spriteState.highlightedSprite, Is.Null);
            }
        }

        [Test]
        public void MixedAndDisabledPreferencesRenderWithoutChangingTheStoredPreference()
        {
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Ui/Ship/ShipUi.prefab"));
            try
            {
                ShipAutoPowerButtonUi control = root.GetComponentInChildren<ShipAutoPowerButtonUi>(true);
                SerializedObject serialized = new SerializedObject(control);
                Button button = (Button)serialized.FindProperty("button").objectReferenceValue;
                TMP_Text label = (TMP_Text)serialized.FindProperty("label").objectReferenceValue;
                // The compact toggle always reads "A"; the highlight carries on/mixed state (b3bf8a4f3).
                GameObject highlight = (GameObject)serialized.FindProperty("activeHighlight").objectReferenceValue;
                ShipAutoPowerUiState mixed = new ShipAutoPowerUiState(3, 2, true, false);
                control.Configure(mixed, () => { });
                Assert.That(control.gameObject.activeSelf, Is.True);
                Assert.That(button.interactable, Is.True);
                Assert.That(label.text, Is.EqualTo("A"));
                Assert.That(highlight.activeSelf, Is.True, "Mixed selections show the highlight.");
                Assert.That(mixed.Description, Does.Contain("enable"));

                ShipAutoPowerUiState disabled = new ShipAutoPowerUiState(3, 2, false, false);
                control.Configure(disabled, () => { });
                Assert.That(button.interactable, Is.False);
                Assert.That(disabled.Mixed, Is.True);
                Assert.That(label.text, Is.EqualTo("A"));
                Assert.That(highlight.activeSelf, Is.False, "Disabled in Settings shows no highlight.");
                Assert.That(disabled.Description, Does.Contain("Disabled in Settings"));

                ShipAutoPowerUiState selected = new ShipAutoPowerUiState(1, 1, true, true);
                control.Configure(selected, () => { });
                Assert.That(selected.Description, Does.Contain("paused while individually selected"));
                Assert.That(label.text, Is.EqualTo("A"));
                Assert.That(highlight.activeSelf, Is.True, "An enabled ship shows the highlight.");
                control.Configure(new ShipAutoPowerUiState(0, 0, true, false), () => { });
                Assert.That(control.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
