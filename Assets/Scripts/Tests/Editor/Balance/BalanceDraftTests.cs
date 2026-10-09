using System;
using System.Collections.Generic;
using EmpireAtWar.Editor.Balance;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceDraftTests
    {
        [Test]
        public void SharedCells_UseOneChangeAndUndoRedo()
        {
            BalanceDraft draft = new BalanceDraft();
            BalanceSnapshot shared = new BalanceSnapshot("guid/weapon/7/damage", "float", "100");
            draft.Set(shared, "120");
            draft.Set(shared, "150");
            Assert.That(draft.Changes.Count, Is.EqualTo(1));
            Assert.That(draft.Value(shared.Key, "100"), Is.EqualTo("150"));
            Assert.That(draft.Changes[0].Before, Is.EqualTo("100"));
            draft.Undo(); Assert.That(draft.Value(shared.Key, "100"), Is.EqualTo("120"));
            draft.Redo(); Assert.That(draft.Value(shared.Key, "100"), Is.EqualTo("150"));
        }

        [Test]
        public void DomainReload_RetainsDraftHistoryPinsFiltersAndSelection()
        {
            BalanceWindowState state = new BalanceWindowState { Faction = "Empire", SelectedUnit = "Empire/ships/201", LeftWidth = 287 };
            state.Pins.Add("Republic/ships/1");
            state.Draft.Set(new BalanceSnapshot("asset/Hull", "float", "100"), "125");
            BalanceWindowState restored = JsonUtility.FromJson<BalanceWindowState>(JsonUtility.ToJson(state));
            Assert.That(restored.Pins, Is.EqualTo(state.Pins));
            Assert.That(restored.Faction, Is.EqualTo("Empire"));
            Assert.That(restored.SelectedUnit, Is.EqualTo(state.SelectedUnit));
            Assert.That(restored.LeftWidth, Is.EqualTo(287));
            restored.Draft.Undo(); Assert.That(restored.Draft.Changes, Is.Empty);
        }

        [Test]
        public void PresetSubset_ReplacesOnlyDeclaredScope()
        {
            BalanceDraft draft = new BalanceDraft();
            BalanceSnapshot a = new BalanceSnapshot("a", "s", "1"), b = new BalanceSnapshot("b", "s", "2");
            draft.Set(a, "3"); draft.Set(b, "4");
            draft.ReplaceScope(new[] { new BalanceSnapshot("a", "s", "5") }, new Dictionary<string, BalanceSnapshot> { ["a"] = a, ["b"] = b });
            Assert.That(draft.Value("a", "1"), Is.EqualTo("5"));
            Assert.That(draft.Value("b", "2"), Is.EqualTo("4"));
            Assert.Throws<InvalidOperationException>(() => draft.ReplaceScope(new[] { new BalanceSnapshot("missing", "s", "5") }, new Dictionary<string, BalanceSnapshot>()));
            Assert.That(draft.Changes.Count, Is.EqualTo(2));
        }

        [Test]
        public void BulkMixedValues_RoundsIntegersAndRejectsInvalidValues()
        {
            BalanceField integer = new BalanceField { Kind = BalanceValueKind.Integer, Minimum = 0 };
            Assert.That(BalanceValue.Bulk(integer, "3", "Multiply", 1.5), Is.EqualTo("5"));
            Assert.That(BalanceValue.Bulk(integer, "7", "Add", 0.5), Is.EqualTo("8"));
            Assert.Throws<InvalidOperationException>(() => BalanceValue.Bulk(integer, "3", "Add", -8));
            BalanceField floating = new BalanceField { Kind = BalanceValueKind.Float, Positive = true };
            Assert.That(floating.Validate("NaN"), Is.Not.Empty);
            Assert.That(floating.Validate("Infinity"), Is.Not.Empty);
            Assert.That(floating.Validate("0"), Is.Not.Empty);
            BalanceField enumeration = new BalanceField { Kind = BalanceValueKind.Enum, EnumType = typeof(EmpireAtWar.Components.AttackComponent.DamageType) };
            int id = Convert.ToInt32(Enum.GetValues(enumeration.EnumType).GetValue(0));
            Assert.That(BalanceValue.Bulk(enumeration, id.ToString(), "Set", id), Is.EqualTo(id.ToString()));
            Assert.Throws<InvalidOperationException>(() => BalanceValue.Bulk(enumeration, id.ToString(), "Multiply", 2));
        }

        [Test]
        public void SharedScope_ReportsHiddenConsumersExactlyOnce()
        {
            BalanceField field = new BalanceField { Shared = true };
            field.Users.Add(new BalanceUnit { Id = "1", Name = "A", Faction = "Empire" });
            field.Users.Add(new BalanceUnit { Id = "2", Name = "B", Faction = "Republic" });
            field.Users.Add(field.Users[0]);
            Assert.That(field.ScopeLabel, Is.EqualTo("Shared across factions — Empire, Republic · 2 units"));
            field.Users.RemoveAll(unit => unit.Faction == "Republic");
            Assert.That(field.ScopeLabel, Is.EqualTo("Shared profile — Empire · 1 units"));
            field.Users.Clear(); Assert.That(field.ScopeLabel, Is.EqualTo("Global profile — no current users"));
        }
    }
}
