using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.Balance;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class BattleSimulatorSetupView : VisualElement
    {
        private readonly BattleSetup _setup;
        private readonly BalanceRegistration _registry;
        private readonly Action _changed;
        private readonly Dictionary<string, string> _errors;
        private readonly Dictionary<string, string> _warnings;

        public BattleSimulatorSetupView(BattleSetup setup, BalanceRegistration registry, Dictionary<string, string> errors,
            Dictionary<string, string> warnings, Action changed)
        {
            _setup = setup; _registry = registry; _changed = changed; _errors = errors; _warnings = warnings;
            AddToClassList("sim-setup");
            var sides = new VisualElement(); sides.AddToClassList("sim-sides"); Add(sides);
            sides.Add(Side("Side A", setup.SideA, setup.PolicyA, value => setup.PolicyA = value));
            sides.Add(Side("Side B", setup.SideB, setup.PolicyB, value => setup.PolicyB = value));
            var settings = new VisualElement(); settings.AddToClassList("sim-settings"); Add(settings);
            var source = new Toggle("Balance draft") { value = setup.UseDraft, tooltip = "Off: live assets. On: snapshot of the saved Balance Editor draft." };
            source.RegisterValueChangedCallback(e => { setup.UseDraft = e.newValue; changed(); }); settings.Add(source);
            Int(settings, "Seed", setup.Seed, value => setup.Seed = value);
            Int(settings, "Runs", setup.Runs, value => setup.Runs = value);
            Float(settings, "Time limit (s)", setup.TimeLimit, value => setup.TimeLimit = value);
            Float(settings, "Distance (-1 = range)", setup.Distance, value => setup.Distance = value);
            var alpha = new Toggle("Alpha strike") { value = setup.AlphaStrike };
            alpha.RegisterValueChangedCallback(e => { setup.AlphaStrike = e.newValue; changed(); }); settings.Add(alpha);
            var focus = new EnumField("Subsystem focus", setup.HardPointFocus);
            focus.RegisterValueChangedCallback(e => { setup.HardPointFocus = (HardPointType)e.newValue; changed(); }); settings.Add(focus);
            var presets = new VisualElement(); presets.AddToClassList("sim-presets"); Add(presets);
            presets.Add(new Button(() =>
            {
                if (setup.SideA.Count == 0) return;
                var selected = setup.SideA[0]; selected.Quantity = 1;
                setup.SideA.RemoveRange(1, setup.SideA.Count - 1);
                setup.SideB.Clear(); setup.SideB.Add(new BattleFleetEntry { UnitId = selected.UnitId, ArcFactor = selected.ArcFactor });
                changed(); RebuildSides(sides);
            }) { text = "1v1 mirror" });
            presets.Add(new Button(() =>
            {
                if (setup.SideA.Count == 0 || setup.SideB.Count == 0) return;
                setup.SideA.RemoveRange(1, setup.SideA.Count - 1); setup.SideA[0].Quantity = 1;
                setup.SideB.RemoveRange(1, setup.SideB.Count - 1);
                setup.SideB[0].Quantity = Math.Min(100, setup.SideB[0].Quantity + 1);
                changed(); RebuildSides(sides);
            }) { text = "1 vs N: add one opponent", tooltip = "Repeat and run to find a break-even fleet size." });
        }

        private void RebuildSides(VisualElement sides)
        {
            sides.Clear();
            sides.Add(Side("Side A", _setup.SideA, _setup.PolicyA, value => _setup.PolicyA = value));
            sides.Add(Side("Side B", _setup.SideB, _setup.PolicyB, value => _setup.PolicyB = value));
        }

        private VisualElement Side(string title, List<BattleFleetEntry> entries, BattleTargetPolicy policy, Action<BattleTargetPolicy> setPolicy)
        {
            var panel = new VisualElement(); panel.AddToClassList("sim-side");
            panel.Add(new Label(title) { name = title == "Side A" ? "sim-side-a" : "sim-side-b" });
            var targeting = new EnumField("Target", policy);
            targeting.RegisterValueChangedCallback(e => { setPolicy((BattleTargetPolicy)e.newValue); _changed(); }); panel.Add(targeting);
            var search = new ToolbarSearchField(); panel.Add(search);
            var faction = new DropdownField("Faction", new[] { "All Factions" }.Concat(_registry.Units.Select(u => u.Faction).Distinct()).ToList(), 0); panel.Add(faction);
            var kind = new DropdownField("Kind", new List<string> { "All", "Ship", "Squadron" }, 0); panel.Add(kind);
            var classes = new DropdownField("Class", new[] { "All Classes" }.Concat(_registry.Units.Select(u => u.Class).Distinct()).ToList(), 0); panel.Add(classes);
            var picker = new DropdownField("Unit"); panel.Add(picker);
            var matches = new List<BalanceUnit>();
            void Filter()
            {
                matches = _registry.Units.Where(u => (faction.value == "All Factions" || u.Faction == faction.value) &&
                    (kind.value == "All" || u.Kind.ToString() == kind.value) && (classes.value == "All Classes" || u.Class == classes.value) &&
                    u.Caption.IndexOf(search.value ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                picker.choices = matches.Select(u => u.Caption).ToList();
                picker.index = matches.Count == 0 ? -1 : 0;
            }
            search.RegisterValueChangedCallback(_ => Filter()); faction.RegisterValueChangedCallback(_ => Filter());
            kind.RegisterValueChangedCallback(_ => Filter()); classes.RegisterValueChangedCallback(_ => Filter()); Filter();
            var rows = new VisualElement();
            void RefreshRows()
            {
                rows.Clear();
                foreach (BattleFleetEntry entry in entries.ToArray())
                {
                    var unit = _registry.Units.FirstOrDefault(u => u.Id == entry.UnitId);
                    var row = new VisualElement(); row.AddToClassList("sim-unit"); rows.Add(row);
                    var caption = new Label(unit == null ? "Missing unit: " + entry.UnitId : unit.Caption);
                    caption.AddToClassList("sim-faction-" + (unit == null ? "unknown" : unit.Faction.ToLowerInvariant())); row.Add(caption);
                    Int(row, "Quantity", entry.Quantity, value => entry.Quantity = value);
                    var arc = new Slider("Guns bearing", 0, 1) { value = entry.ArcFactor, showInputField = true };
                    arc.RegisterValueChangedCallback(e => { entry.ArcFactor = e.newValue; _changed(); }); row.Add(arc);
                    if (_errors.TryGetValue(entry.UnitId, out string error)) row.Add(new HelpBox(error, HelpBoxMessageType.Error));
                    if (_warnings.TryGetValue(entry.UnitId, out string warning) && warning.Length > 0) row.Add(new HelpBox(warning, HelpBoxMessageType.Warning));
                    row.Add(new Button(() => { entries.Remove(entry); _changed(); RefreshRows(); }) { text = "Remove" });
                }
            }
            panel.Add(new Button(() =>
            {
                if (picker.index < 0 || picker.index >= matches.Count) return;
                entries.Add(new BattleFleetEntry { UnitId = matches[picker.index].Id }); _changed(); RefreshRows();
            }) { text = "Add unit" });
            panel.Add(rows); RefreshRows(); return panel;
        }

        private void Int(VisualElement parent, string label, int value, Action<int> set)
        {
            var field = new IntegerField(label) { value = value, isDelayed = true };
            field.RegisterValueChangedCallback(e => { set(e.newValue); _changed(); }); parent.Add(field);
        }

        private void Float(VisualElement parent, string label, float value, Action<float> set)
        {
            var field = new FloatField(label) { value = value, isDelayed = true };
            field.RegisterValueChangedCallback(e => { set(e.newValue); _changed(); }); parent.Add(field);
        }
    }
}
