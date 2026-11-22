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
    [Category("Balance")]
    public sealed class BalanceShieldRegenTiersViewTests
    {
        [Test]
        public void ShipFactions_DoNotShareShieldRegenTierTables()
        {
            BalanceRegistration registry = BalanceInventory.Build();
            Assert.That(registry.Errors, Is.Empty);
            // A ship data asset listed in several faction rosters (e.g. Tector) necessarily keeps one table.
            var sharedShips = registry.Units
                .GroupBy(unit => unit.Data)
                .Where(group => group.Select(unit => unit.Faction).Distinct().Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();
            var shared = registry.Units
                .Where(unit => !sharedShips.Contains(unit.Data))
                .Where(unit => unit.Fields.ContainsKey(BalanceUnitAdapter.SHIELD_REGEN_TIER_VALUE + "0"))
                .GroupBy(unit => registry.Fields[unit.Fields[BalanceUnitAdapter.SHIELD_REGEN_TIER_VALUE + "0"]].Target.name)
                .Where(group => group.Select(unit => unit.Faction).Distinct().Count() > 1)
                .Select(group => group.Key + ": " + string.Join(", ", group.Select(unit => unit.Faction).Distinct()))
                .ToList();
            Assert.That(shared, Is.Empty, "Each faction needs its own shield regen tier table.");
        }

        // The grid is hosted in a window: a detached field's value setter raises no ChangeEvent, so the draft edit would never fire.
        [UnityTest]
        public IEnumerator Grid_ShowsEveryTableAndTierAndEditsDraft()
        {
            BalanceRegistration registry = BalanceInventory.Build();
            BalanceWindowState state = new BalanceWindowState();
            int refreshes = 0;
            BalanceEditorController controller = new BalanceEditorController(state, () => refreshes++);
            EditorWindow host = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                VisualElement panel = new VisualElement();
                host.rootVisualElement.Add(panel);
                BalanceShieldRegenTiersView.Build(panel, registry, controller, new BalanceDraftUsage(registry, state.Draft));
                host.Show();
                yield return null;

                int tables = registry.Fields.Values.Where(BalanceShieldRegenTiersView.Contains).Select(field => field.Target).Distinct().Count();
                var rows = panel.Q("shield-regen-tiers").Query<VisualElement>(className: "balance-regen-row").ToList();
                Assert.That(rows.Count, Is.EqualTo(1 + System.Enum.GetValues(typeof(EmpireAtWar.Entities.Ship.Data.ShieldRegenTier)).Length));
                Assert.That(rows[1].Query<VisualElement>(className: "balance-regen-cell").ToList().Count, Is.EqualTo(tables));

                FloatField cell = rows[1].Q<FloatField>();
                Assert.That(cell, Is.Not.Null);
                cell.value += 1;
                Assert.That(state.Draft.Changes, Is.Not.Empty);
            }
            finally { host.Close(); }
        }
    }
}
