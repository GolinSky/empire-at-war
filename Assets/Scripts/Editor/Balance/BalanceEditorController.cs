using System;
using System.Collections.Generic;
using System.Linq;

namespace EmpireAtWar.Editor.Balance
{
    // The only writer of BalanceWindowState. Views read State and call these methods.
    // Methods that change what is drawn request a redraw; filter and selection edits that views
    // apply in place do not.
    public sealed class BalanceEditorController
    {
        private readonly Action _redraw;

        public BalanceWindowState State { get; }

        public BalanceEditorController(BalanceWindowState state, Action redraw)
        {
            State = state;
            _redraw = redraw;
        }

        public void Redraw() => _redraw();

        public void Edit(BalanceField field, string value)
        {
            State.Draft.Set(field.Snapshot(), value);
            _redraw();
        }

        public void RemoveChange(string key)
        {
            State.Draft.Remove(key);
            _redraw();
        }

        public void KeepChange(BalanceField field)
        {
            State.Draft.Rebase(field.Snapshot());
            _redraw();
        }

        public void Undo()
        {
            State.Draft.Undo();
            _redraw();
        }

        public void Redo()
        {
            State.Draft.Redo();
            _redraw();
        }

        public void DiscardDraft()
        {
            State.Draft.Discard();
            _redraw();
        }

        public void ShowTab(BalanceTab tab)
        {
            State.Tab = tab;
            _redraw();
        }

        public void ShowUnitTab(BalanceUnitTab tab)
        {
            State.UnitTab = tab;
            _redraw();
        }

        public void ShowStatsCategory(BalanceStatsCategory category)
        {
            State.UnitGroup = category;
            _redraw();
        }

        public void ShowCombatTab(BalanceCombatTab tab)
        {
            State.CombatTab = tab;
            _redraw();
        }

        public void SearchCombat(string text)
        {
            State.CombatSearch = text;
            _redraw();
        }

        public void SelectUnit(string id)
        {
            State.SelectedUnit = id;
            State.SelectedField = "";
            State.ShowDetails = false;
            _redraw();
        }

        public void OpenUnit(string id)
        {
            State.UnitDetailsOpen = true;
            SelectUnit(id);
        }

        public void CloseUnit()
        {
            State.UnitDetailsOpen = false;
            State.ShowDetails = false;
            _redraw();
        }

        public void ShowField(BalanceField field)
        {
            State.SelectedField = field.Key;
            State.ShowDetails = true;
            _redraw();
        }

        public void CloseField()
        {
            State.ShowDetails = false;
            _redraw();
        }

        public void AddPins(IEnumerable<string> ids)
        {
            State.Pins.AddRange(ids.Where(id => !State.Pins.Contains(id)).Distinct().ToList());
            _redraw();
        }

        public void RemovePin(string id)
        {
            State.Pins.Remove(id);
            _redraw();
        }

        public void TogglePin(string id)
        {
            if (!State.Pins.Remove(id)) State.Pins.Add(id);
            _redraw();
        }

        public void RemoveSelectedPins()
        {
            State.Pins.RemoveAll(State.CompareSelection.Contains);
            State.CompareSelection.Clear();
            State.CompareFocusPending = true;
            _redraw();
        }

        public void MovePin(string source, string target, bool after)
        {
            if (BalanceCompareOrder.Move(State, source, target, after)) _redraw();
        }

        public void SortCompare(BalanceCompareSort sort)
        {
            State.CompareSort = sort;
            _redraw();
        }

        // Called while building the comparison: applies the chosen order and drops selections of removed cards.
        public void PrepareCompare(BalanceRegistration registry)
        {
            BalanceCompareOrder.Sort(registry, State);
            State.CompareSelection.RemoveAll(id => !State.Pins.Contains(id));
        }

        public void SetComparePickerOpen(bool open) => State.ComparePickerOpen = open;

        public void SearchCompare(string text) => State.CompareSearch = text;

        public void FilterCompareFaction(string faction) => State.CompareFaction = faction;

        public bool TakeCompareFocus()
        {
            bool pending = State.CompareFocusPending;
            State.CompareFocusPending = false;
            return pending;
        }

        public void SelectAllCompared(IReadOnlyList<string> order) => State.CompareSelection = order.ToList();

        // Plain click selects one card, additive toggles one, range extends from the anchor in display order.
        public void SelectCompared(IReadOnlyList<string> order, string id, bool additive, bool range)
        {
            List<string> selection = State.CompareSelection;
            int anchor = order.ToList().IndexOf(State.CompareSelectionAnchor);
            if (!additive) selection.Clear();
            if (range && anchor >= 0)
            {
                int target = order.ToList().IndexOf(id);
                for (int index = Math.Min(anchor, target); index <= Math.Max(anchor, target); index++)
                {
                    if (!selection.Contains(order[index])) selection.Add(order[index]);
                }
                return;
            }

            State.CompareSelectionAnchor = id;
            if (!selection.Remove(id)) selection.Add(id);
        }

        public void FilterRosterFaction(string faction) => State.Faction = faction;

        public void FilterRosterKind(BalanceUnitKind kind) => State.Kind = kind;

        public void FilterRosterClass(string unitClass) => State.Class = unitClass;

        public void SearchRoster(string text) => State.Search = text;
    }
}
