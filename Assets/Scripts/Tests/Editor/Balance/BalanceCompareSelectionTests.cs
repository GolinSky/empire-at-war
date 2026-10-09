using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceCompareSelectionTests
    {
        [Test]
        public void Selection_ShiftRangesFollowDisplayedOrderAndRetainAnchor()
        {
            VisualElement grid = new VisualElement();
            for (int i = 0; i < 10; i++) grid.Add(new VisualElement { userData = i.ToString() });
            BalanceWindowState state = new BalanceWindowState();
            BalanceEditorController controller = new BalanceEditorController(state, () => { });
            BalanceCompareSelection.Select(grid, controller, "0", false, false);
            BalanceCompareSelection.Select(grid, controller, "9", false, true);
            Assert.That(state.CompareSelection, Is.EqualTo(Enumerable.Range(0, 10).Select(i => i.ToString())));
            BalanceCompareSelection.Select(grid, controller, "4", false, true);
            Assert.That(state.CompareSelection, Is.EqualTo(new[] { "0", "1", "2", "3", "4" }));
            Assert.That(state.CompareSelectionAnchor, Is.EqualTo("0"));
            BalanceCompareSelection.Select(grid, controller, "9", false, false);
            BalanceCompareSelection.Select(grid, controller, "6", false, true);
            Assert.That(state.CompareSelection, Is.EqualTo(new[] { "6", "7", "8", "9" }));
            grid.Insert(0, grid.ElementAt(9));
            BalanceCompareSelection.Select(grid, controller, "1", false, true);
            Assert.That(state.CompareSelection, Is.EqualTo(new[] { "9", "0", "1" }));
            BalanceCompareSelection.Select(grid, controller, "4", true, false);
            BalanceCompareSelection.Select(grid, controller, "6", true, true);
            Assert.That(state.CompareSelection, Is.EqualTo(new[] { "9", "0", "1", "4", "5", "6" }));
            grid.Remove(grid.Children().Single(slot => (string)slot.userData == "4"));
            BalanceCompareSelection.Select(grid, controller, "7", false, true);
            Assert.That(state.CompareSelection, Is.EqualTo(new[] { "7" }));
            Assert.That(state.CompareSelectionAnchor, Is.EqualTo("7"));
        }

        [UnityTest]
        public IEnumerator Selection_ClickCtrlClickSelectAllAndDeletePersistAcrossRedraws()
        {
            BalanceRegistration registry = new BalanceRegistration();
            for (int i = 0; i < 3; i++)
                registry.Units.Add(new BalanceUnit { Id = i.ToString(), Name = "Ship " + i, Kind = BalanceUnitKind.Ship, Class = "Frigate", Faction = "Empire" });
            BalanceWindowState state = new BalanceWindowState { Pins = new List<string> { "0", "1", "2" } };
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            void Build()
            {
                host.rootVisualElement.Clear();
                BalanceCompareView.Build(host.rootVisualElement, registry, new BalanceEditorController(state, Build), false);
            }
            VisualElement Card(string id) => host.rootVisualElement.Query<VisualElement>(className: "balance-compare-card").ToList().Single(card => (string)card.userData == id);
            try
            {
                host.position = new Rect(100, 100, 960, 700);
                host.rootVisualElement.AddToClassList("balance-root");
                host.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
                Build(); host.Show(); yield return null; yield return null;
                Click(Card("0"), false);
                yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "0" }));
                Assert.That(Card("0").ClassListContains("balance-compare-selected"), Is.True);
                ScrollView viewport = host.rootVisualElement.Query<ScrollView>().ToList().Single(scroll => scroll.viewDataKey == "balance-compare-scroll");
                viewport.ScrollTo(Card("1")); yield return null; yield return null;
                Click(Card("1"), false, true);
                yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "0", "1" }));
                Assert.That(Card("1").ClassListContains("balance-compare-selected"), Is.True);
                viewport.ScrollTo(Card("0")); yield return null; yield return null;
                Click(Card("0"), true);
                yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "1" }));
                viewport.ScrollTo(Card("2")); yield return null; yield return null;
                Click(Card("2"), false);
                yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "2" }));
                state.CompareSort = BalanceCompareSort.Class; Build();
                yield return null; yield return null;
                Assert.That(Card("2").ClassListContains("balance-compare-selected"), Is.True);
                Key(Card("2"), KeyCode.A, true);
                yield return null;
                Assert.That(state.CompareSelection, Is.EquivalentTo(state.Pins));
                Assert.That(host.rootVisualElement.Query<VisualElement>(className: "balance-compare-selected").ToList().Count, Is.EqualTo(3));
                viewport = host.rootVisualElement.Query<ScrollView>().ToList().Single(scroll => scroll.viewDataKey == "balance-compare-scroll");
                viewport.ScrollTo(Card("0")); yield return null; yield return null;
                Click(Card("0"), false); yield return null;
                viewport.ScrollTo(Card("2")); yield return null; yield return null;
                Click(Card("2"), true); yield return null;
                Key(Card("2"), KeyCode.Delete, false);
                yield return null;
                Assert.That(state.Pins, Is.EqualTo(new[] { "1" }));
                Assert.That(state.CompareSelection, Is.Empty);
                Assert.That(host.rootVisualElement.Query<VisualElement>(className: "balance-compare-card").ToList().Count, Is.EqualTo(1));
                yield return null; yield return null;
                VisualElement grid = host.rootVisualElement.Q("compare-grid");
                Assert.That(host.rootVisualElement.panel.focusController.focusedElement, Is.SameAs(grid));
                Key(grid, KeyCode.A, true);
                Key(grid, KeyCode.Delete, false);
                Assert.That(state.Pins, Is.Empty);
                Assert.That(host.rootVisualElement.Q("compare-grid"), Is.Null);
                Assert.That(state.ComparePickerOpen, Is.True);
                Assert.That(host.rootVisualElement.Q<ListView>().itemsSource.Count, Is.EqualTo(3));
                Assert.That(state.Draft.Changes, Is.Empty);
            }
            finally { host.Close(); }
        }

        [UnityTest]
        public IEnumerator Selection_LeavesFocusedControlsAndButtonsAlone()
        {
            BalanceRegistration registry = new BalanceRegistration();
            registry.Units.Add(new BalanceUnit { Id = "unit", Name = "Ship", Kind = BalanceUnitKind.Ship, Class = "Frigate", Faction = "Empire" });
            BalanceWindowState state = new BalanceWindowState { Pins = new List<string> { "unit" } };
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                host.position = new Rect(100, 100, 960, 700);
                host.rootVisualElement.AddToClassList("balance-root");
                host.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
                BalanceCompareView.Build(host.rootVisualElement, registry, new BalanceEditorController(state, () => { }), false);
                host.Show(); yield return null; yield return null;
                VisualElement card = host.rootVisualElement.Q(className: "balance-compare-card");
                ScrollView viewport = host.rootVisualElement.Query<ScrollView>().ToList().Single(scroll => scroll.viewDataKey == "balance-compare-scroll");
                viewport.ScrollTo(card); yield return null; yield return null;
                Click(card, false); yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "unit" }));
                TextField text = new TextField { value = "editable" }; card.Add(text);
                FloatField number = new FloatField { value = 42 }; card.Add(number);
                Button button = new Button(); card.Add(button);
                yield return null; yield return null;
                Click(text, true);
                yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "unit" }));
                Click(button, true);
                yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "unit" }));
                Key(text, KeyCode.A, true);
                Key(number, KeyCode.Delete, false);
                Key(button, KeyCode.Delete, false);
                yield return null;
                Assert.That(state.Pins, Is.EqualTo(new[] { "unit" }));
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "unit" }));
                text.value = "changed"; number.value = 10;
                Assert.That(text.value, Is.EqualTo("changed"));
                Assert.That(number.value, Is.EqualTo(10));
            }
            finally { host.Close(); }
        }

        private static void Click(VisualElement target, bool control, bool shift = false)
        {
            Vector2 position = target.ClassListContains("balance-compare-card")
                ? target.worldBound.min + new Vector2(3, 3) : target.worldBound.center;
            EventModifiers modifiers = control ? EventModifiers.Control : EventModifiers.None;
            if (shift) modifiers |= EventModifiers.Shift;
            using (PointerDownEvent evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = position, button = 0, modifiers = modifiers }))
                target.SendEvent(evt);
            using (PointerUpEvent evt = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = position, button = 0, modifiers = modifiers }))
                target.SendEvent(evt);
            VisualElement captured = target.panel.GetCapturingElement(PointerId.mousePointerId) as VisualElement;
            if (captured != null) captured.ReleasePointer(PointerId.mousePointerId);
        }

        private static void Key(VisualElement target, KeyCode key, bool control)
        {
            target.Focus();
            using (KeyDownEvent evt = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = key, modifiers = control ? EventModifiers.Control : EventModifiers.None }))
                target.SendEvent(evt);
        }
    }
}
