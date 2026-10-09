using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareSelection
    {
        public static void Bind(VisualElement grid, BalanceWindowState state, Action refresh)
        {
            grid.focusable = true;
            if (state.CompareFocusPending) { grid.schedule.Execute(grid.Focus); state.CompareFocusPending = false; }
            grid.RegisterCallback<KeyDownEvent>(evt =>
            {
                for (VisualElement element = evt.target as VisualElement; element != grid; element = element.parent)
                    if (element is Button || element.ClassListContains("unity-base-field")) return;
                if (evt.ctrlKey && evt.keyCode == KeyCode.A)
                {
                    state.CompareSelection = grid.Children().Select(slot => (string)slot.userData).ToList();
                    Update(grid, state);
                }
                else if (evt.keyCode == KeyCode.Delete && state.CompareSelection.Count > 0)
                {
                    state.Pins.RemoveAll(state.CompareSelection.Contains);
                    state.CompareSelection.Clear();
                    Update(grid, state);
                    state.CompareFocusPending = true;
                    refresh();
                }
                else return;
                evt.StopPropagation();
                evt.PreventDefault();
            });
        }

        public static void Select(VisualElement grid, BalanceWindowState state, string id, bool additive, bool range)
        {
            var order = grid.Children().Select(slot => (string)slot.userData).ToList();
            int anchor = order.IndexOf(state.CompareSelectionAnchor);
            if (!additive) state.CompareSelection.Clear();
            if (range && anchor >= 0)
            {
                int target = order.IndexOf(id);
                for (int index = Math.Min(anchor, target); index <= Math.Max(anchor, target); index++)
                    if (!state.CompareSelection.Contains(order[index])) state.CompareSelection.Add(order[index]);
            }
            else
            {
                state.CompareSelectionAnchor = id;
                if (state.CompareSelection.Contains(id)) state.CompareSelection.Remove(id);
                else state.CompareSelection.Add(id);
            }
            Update(grid, state);
        }

        public static void Update(VisualElement grid, BalanceWindowState state)
        {
            foreach (VisualElement card in grid.Query<VisualElement>(className: "balance-compare-card").ToList())
                card.EnableInClassList("balance-compare-selected", state.CompareSelection.Contains((string)card.userData));
        }
    }
}
