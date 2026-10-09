using System;
using System.Collections;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceHardpointsPopupTests
    {
        [UnityTest]
        public IEnumerator Popup_ShowsAllHardpointsAndKeepsDraftEditingOpen()
        {
            BalanceRegistration registry = BalanceInventory.Build();
            Assert.That(registry.Errors, Is.Empty);
            BalanceUnit unit = registry.Units.First(entry => entry.Kind == BalanceUnitKind.Ship && entry.Mounts.Count > 12);
            BalanceWindowState state = new BalanceWindowState { Pins = new System.Collections.Generic.List<string> { unit.Id } };
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            EditorWindow popupHost = ScriptableObject.CreateInstance<EditorWindow>();
            int edits = 0;
            BalanceEditorController controller = null;
            controller = new BalanceEditorController(state, () =>
            {
                edits++;
                host.rootVisualElement.Clear();
                BalanceCompareView.Build(host.rootVisualElement, registry, controller, false);
            });
            BalanceHardpointsPopup popup = new BalanceHardpointsPopup(unit, registry, controller);
            try
            {
                host.position = new Rect(100, 100, 960, 700);
                host.rootVisualElement.AddToClassList("balance-root");
                host.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
                BalanceCompareView.Build(host.rootVisualElement, registry, new BalanceEditorController(state, () => { }), false);
                host.Show(); yield return null; yield return null;
                Button button = host.rootVisualElement.Q<Button>("compare-view-hardpoints");
                Assert.That(button.text, Is.EqualTo("View hardpoints (" + unit.Mounts.Distinct().Count() + ")"));
                Assert.That(host.rootVisualElement.Q(className: "balance-compare-hardpoint"), Is.Null);
                // Use a persistent host so desktop focus changes cannot dismiss the popup during assertions.
                typeof(PopupWindowContent).GetProperty("editorWindow").SetValue(popup, popupHost);
                popupHost.minSize = popup.GetWindowSize();
                popupHost.maxSize = popup.GetWindowSize();
                popupHost.position = new Rect(100, 100, popup.GetWindowSize().x, popup.GetWindowSize().y);
                popup.OnOpen();
                popupHost.Show();
                yield return null; yield return null;
                VisualElement root = popup.editorWindow.rootVisualElement;
                Assert.That(popup.editorWindow.position.width, Is.EqualTo(620).Within(2));
                Assert.That(popup.editorWindow.position.height, Is.LessThanOrEqualTo(562));
                Assert.That(root.Query<VisualElement>(className: "balance-compare-hardpoint").ToList().Count, Is.EqualTo(unit.Mounts.Distinct().Count()));
                Assert.That(root.Q<ScrollView>("hardpoints-popup-list").worldBound.height, Is.GreaterThan(350));
                BalanceField mount = registry.Fields.Values.First(field => !field.SharedMountSource && field.Stat == "WeaponType" && unit.Mounts.Contains(field.Target));
                string before = mount.Read();
                VisualElement row = root.Query<VisualElement>(className: "balance-compare-edit").ToList().Single(element => (string)element.userData == mount.Key);
                DropdownField weapon = row.Q<DropdownField>();
                string next = weapon.choices.First(choice => Convert.ToInt32(Enum.Parse(mount.EnumType, choice)).ToString() != before);
                Assert.That(weapon.panel, Is.Not.Null, "Popup control must be attached.");
                weapon.value = next;
                Assert.That(edits, Is.EqualTo(1), "Weapon change must reach the popup callback: " + before + " → " + next);
                yield return null; yield return null;
                Assert.That(state.Draft.Changes.Any(change => change.Key == mount.Key), Is.True);
                Assert.That(mount.Read(), Is.EqualTo(before));
                Assert.That(popup.editorWindow.rootVisualElement.Q<ScrollView>("hardpoints-popup-list"), Is.Not.Null);
                row = root.Query<VisualElement>(className: "balance-compare-edit").ToList().Single(element => (string)element.userData == mount.Key);
                Assert.That(row.Q<DropdownField>().value, Is.EqualTo(BalanceFieldView.Display(mount, mount.DraftValue(state.Draft))));
            }
            finally
            {
                popupHost.Close();
                host.Close();
            }
        }
    }
}
