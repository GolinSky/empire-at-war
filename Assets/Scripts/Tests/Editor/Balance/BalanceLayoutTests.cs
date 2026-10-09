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
    public sealed class BalanceLayoutTests
    {
        [UnityTest]
        public IEnumerator LongRosterNames_KeepPinsVisibleAtDefaultAndMinimumWidths()
        {
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                host.position = new Rect(100, 100, 960, 550);
                host.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
                BalanceRegistration registry = new BalanceRegistration();
                registry.Units.Add(new BalanceUnit { Id = "test/1", Name = "Victory II Star Destroyer — Advanced Loadout", Kind = BalanceUnitKind.Ship, Class = "HeavyCapital", Faction = "Empire" });
                VisualElement roster = new VisualElement(); roster.style.height = 500; host.rootVisualElement.Add(roster);
                BalanceRosterView.Build(roster, registry, new BalanceEditorController(new BalanceWindowState(), () => { }));
                host.Show();
                foreach (int width in new[] { 255, 220 })
                {
                    roster.style.width = width;
                    yield return null; yield return null;
                    VisualElement row = roster.Query<VisualElement>(className: "balance-roster-row").ToList().Single(element => element.userData != null);
                    Button pin = row.Q<Button>("unit-pin");
                    Assert.That(pin.worldBound.width, Is.EqualTo(24).Within(1));
                    Assert.That(pin.worldBound.xMax, Is.LessThanOrEqualTo(row.worldBound.xMax));
                    Assert.That(pin.worldBound.xMin, Is.GreaterThanOrEqualTo(row.worldBound.xMin));
                }
            }
            finally { host.Close(); }
        }

        [UnityTest]
        public IEnumerator Compare_AdaptsColumnsToAvailableWidthAndKeepsCardsCompact()
        {
            BalanceRegistration registry = new BalanceRegistration();
            for (int i = 0; i < 9; i++)
                registry.Units.Add(new BalanceUnit { Id = "test/" + i, Name = "Ship " + i, Kind = BalanceUnitKind.Ship, Class = "Frigate", Faction = "Empire" });
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                host.position = new Rect(100, 100, 1280, 720);
                host.rootVisualElement.AddToClassList("balance-root");
                host.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Balance/BalanceEditor.uss"));
                host.Show();
                foreach (int count in new[] { 2, 9 })
                {
                    host.rootVisualElement.Clear();
                    VisualElement panel = new VisualElement(); panel.style.height = 650; host.rootVisualElement.Add(panel);
                    BalanceWindowState state = new BalanceWindowState { Pins = registry.Units.Take(count).Select(unit => unit.Id).ToList() };
                    BalanceCompareView.Build(panel, registry, new BalanceEditorController(state, () => { }), false);
                    foreach (var layout in new[] { (Width: 280, Columns: 1), (Width: 720, Columns: 2), (Width: 1080, Columns: 3), (Width: 1800, Columns: 5), (Width: 720, Columns: 2) })
                    {
                        panel.style.width = layout.Width;
                        yield return null; yield return null;
                        var cards = panel.Query<VisualElement>(className: "balance-compare-card").ToList();
                        VisualElement grid = panel.Q("compare-grid");
                        Assert.That(cards.Count, Is.EqualTo(count));
                        Assert.That(cards[0].worldBound.width, Is.InRange(250, 330));
                        Assert.That(cards.Count(card => Mathf.Abs(card.worldBound.y - cards[0].worldBound.y) < 1),
                            Is.EqualTo(System.Math.Min(count, layout.Columns)), "Available width: " + layout.Width);
                        Assert.That(cards.All(card => card.worldBound.xMin >= grid.worldBound.xMin - 1
                            && card.worldBound.xMax <= grid.worldBound.xMax + 1), Is.True);
                        if (count > layout.Columns)
                            Assert.That(cards[layout.Columns].worldBound.y, Is.GreaterThan(cards[0].worldBound.yMax));
                    }
                    Assert.That(panel.Q<Foldout>(), Is.Null);
                    Assert.That(panel.Query<Label>().ToList().Any(label => label.text.Contains("vs A")), Is.False);
                    Assert.That(panel.Q<Button>("compare-add-unit"), Is.Not.Null);
                }
            }
            finally { host.Close(); }
        }
    }
}
