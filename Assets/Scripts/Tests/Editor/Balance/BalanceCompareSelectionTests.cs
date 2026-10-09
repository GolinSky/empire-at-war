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
        [UnityTest]
        public IEnumerator Selection_ClickCtrlClickSelectAllAndDeletePersistAcrossRedraws()
        {
            BalanceRegistration registry = new BalanceRegistration();
            for (int i = 0; i < 3; i++)
                registry.Units.Add(new BalanceUnit { Id = i.ToString(), Name = "Ship " + i, Kind = "Ship", Class = "Frigate", Faction = "Empire" });
            BalanceWindowState state = new BalanceWindowState { Pins = new List<string> { "0", "1", "2" } };
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            void Build()
            {
                host.rootVisualElement.Clear();
                BalanceCompareView.Build(host.rootVisualElement, registry, state, (_, __) => { }, Build);
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
                Click(Card("1"), true);
                yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "0", "1" }));
                Click(Card("0"), true);
                yield return null;
                Assert.That(state.CompareSelection, Is.EqualTo(new[] { "1" }));
                ScrollView viewport = host.rootVisualElement.Query<ScrollView>().ToList().Single(scroll => scroll.viewDataKey == "balance-compare-scroll");
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
            registry.Units.Add(new BalanceUnit { Id = "unit", Name = "Ship", Kind = "Ship", Class = "Frigate", Faction = "Empire" });
            BalanceWindowState state = new BalanceWindowState { Pins = new List<string> { "unit" } };
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                host.position = new Rect(100, 100, 960, 700);
                host.rootVisualElement.AddToClassList("balance-root");
                host.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
                BalanceCompareView.Build(host.rootVisualElement, registry, state, (_, __) => { }, () => { });
                host.Show(); yield return null; yield return null;
                VisualElement card = host.rootVisualElement.Q(className: "balance-compare-card");
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

        private static void Click(VisualElement target, bool control)
        {
            Vector2 position = target.ClassListContains("balance-compare-card")
                ? target.worldBound.min + new Vector2(3, 3) : target.worldBound.center;
            EventModifiers modifiers = control ? EventModifiers.Control : EventModifiers.None;
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
