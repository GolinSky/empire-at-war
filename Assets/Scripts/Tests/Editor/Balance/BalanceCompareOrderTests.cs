using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceCompareOrderTests
    {
        [TestCase(BalanceCompareSort.Price, "Price", "Hull")]
        [TestCase(BalanceCompareSort.HeightLevel, "HeightTier", "HeightTier")]
        [TestCase(BalanceCompareSort.AvailabilityLevel, "AvailableLevel", "Speed")]
        public void NumericOrder_UsesDraftValuesAndLeavesMissingStatsLast(BalanceCompareSort sort, string stat, string property)
        {
            BalanceRegistration registry = new BalanceRegistration();
            List<ShipData> assets = new List<ShipData>();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    ShipData data = ScriptableObject.CreateInstance<ShipData>(); assets.Add(data);
                    using (SerializedObject serialized = new SerializedObject(data))
                    {
                        SerializedProperty value = serialized.FindProperty(BalanceRegistration.Auto(property));
                        if (value.propertyType == SerializedPropertyType.Enum) value.intValue = i + 1;
                        else value.floatValue = i + 1;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    BalanceUnit unit = new BalanceUnit { Id = i.ToString(), Data = data };
                    registry.Units.Add(unit);
                    unit.Fields[stat] = registry.Add(data, unit.Id + "/" + stat, BalanceRegistration.Auto(property),
                        "Test", "Test", "Test", false, new[] { unit }, sort == BalanceCompareSort.HeightLevel ? typeof(ShipHeightTier) : null).Key;
                }
                registry.Units.Add(new BalanceUnit { Id = "missing" });
                BalanceWindowState state = new BalanceWindowState { Pins = new List<string> { "missing", "2", "1", "0" }, CompareSort = sort };
                BalanceCompareOrder.Sort(registry, state);
                Assert.That(state.Pins, Is.EqualTo(new[] { "0", "1", "2", "missing" }));
                BalanceField field = registry.Fields[registry.Units[2].Fields[stat]];
                state.Draft.Set(field.Snapshot(), "0");
                BalanceCompareOrder.Sort(registry, state);
                Assert.That(state.Pins, Is.EqualTo(new[] { "2", "0", "1", "missing" }));
                Assert.That(field.Read(), Is.EqualTo("3"));
                state.CompareSort = BalanceCompareSort.Manual;
                state.Pins.Reverse();
                string[] manual = state.Pins.ToArray();
                BalanceCompareOrder.Sort(registry, state);
                Assert.That(state.Pins, Is.EqualTo(manual));
            }
            finally { foreach (ShipData data in assets) Object.DestroyImmediate(data); }
        }

        [Test]
        public void ClassOrder_GroupsSquadronsAndUsesShipSizeSequence()
        {
            string[] classes = { "Bomber", "Interceptor", "Corvette", "Frigate", "Cruiser", "Capital", "HeavyCapital", "Structure" };
            BalanceRegistration registry = new BalanceRegistration();
            for (int i = 0; i < classes.Length; i++)
                registry.Units.Add(new BalanceUnit { Id = i.ToString(), Class = classes[i], Kind = i < 2 ? "Squadron" : "Ship" });
            BalanceWindowState state = new BalanceWindowState
                { Pins = registry.Units.Select(unit => unit.Id).Reverse().ToList(), CompareSort = BalanceCompareSort.Class };
            BalanceCompareOrder.Sort(registry, state);
            Assert.That(state.Pins, Is.EqualTo(new[] { "1", "0", "2", "3", "4", "5", "6", "7" }));
        }

        [TestCase("a", "c", true, "b,c,a,d")]
        [TestCase("a", "c", false, "b,a,c,d")]
        [TestCase("d", "a", false, "d,a,b,c")]
        [TestCase("d", "a", true, "a,d,b,c")]
        public void Move_InsertsBeforeOrAfterAndSwitchesToManual(string source, string target, bool after, string expected)
        {
            BalanceWindowState state = new BalanceWindowState { Pins = new List<string> { "a", "b", "c", "d" }, CompareSort = BalanceCompareSort.Price };
            Assert.That(BalanceCompareOrder.Move(state, source, target, after), Is.True);
            Assert.That(state.Pins, Is.EqualTo(expected.Split(',')));
            Assert.That(state.CompareSort, Is.EqualTo(BalanceCompareSort.Manual));
        }

        [UnityTest]
        public IEnumerator DragHandle_MovesAcrossRowsAndEscapeCancels()
        {
            BalanceRegistration registry = new BalanceRegistration();
            for (int i = 0; i < 6; i++)
                registry.Units.Add(new BalanceUnit { Id = i.ToString(), Name = "Ship " + i, Kind = "Ship", Class = "Frigate", Faction = "Empire" });
            BalanceWindowState state = new BalanceWindowState { Pins = registry.Units.Select(unit => unit.Id).ToList(), CompareSort = BalanceCompareSort.Class };
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            int refreshes = 0;
            try
            {
                host.position = new Rect(100, 100, 960, 900);
                host.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
                VisualElement panel = new VisualElement(); panel.style.width = 720; panel.style.height = 850; host.rootVisualElement.Add(panel);
                BalanceCompareView.Build(panel, registry, state, (_, __) => { }, () => refreshes++);
                host.Show(); yield return null; yield return null;
                var slots = panel.Query<VisualElement>(className: "balance-compare-slot").ToList();
                VisualElement handle = slots[0].Q("compare-drag-handle");
                Vector2 start = handle.worldBound.center;
                Vector2 drop = slots[3].worldBound.center + new Vector2(30, 0);
                using (PointerDownEvent evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = start, button = 0 }))
                    handle.SendEvent(evt);
                using (PointerMoveEvent evt = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = drop, button = 0 }))
                    handle.SendEvent(evt);
                Assert.That(slots[3].ClassListContains("balance-drop-after"), Is.True);
                using (PointerUpEvent evt = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = drop, button = 0 }))
                    handle.SendEvent(evt);
                Assert.That(state.Pins, Is.EqualTo(new[] { "1", "2", "3", "0", "4", "5" }));
                Assert.That(state.CompareSort, Is.EqualTo(BalanceCompareSort.Manual));
                Assert.That(refreshes, Is.EqualTo(1));
                using (PointerDownEvent evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = start, button = 0 }))
                    handle.SendEvent(evt);
                using (PointerMoveEvent evt = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, mousePosition = drop, button = 0 }))
                    handle.SendEvent(evt);
                using (KeyDownEvent evt = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                    handle.SendEvent(evt);
                using (PointerUpEvent evt = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = drop, button = 0 }))
                    handle.SendEvent(evt);
                Assert.That(refreshes, Is.EqualTo(1));
                Assert.That(slots.Any(slot => slot.ClassListContains("balance-drop-before") || slot.ClassListContains("balance-drop-after") || slot.ClassListContains("balance-dragging")), Is.False);
                EnumField order = panel.Q<EnumField>("compare-order");
                order.value = BalanceCompareSort.Price;
                Assert.That(state.CompareSort, Is.EqualTo(BalanceCompareSort.Price));
                Assert.That(refreshes, Is.EqualTo(2));
            }
            finally { host.Close(); }
        }
    }
}
