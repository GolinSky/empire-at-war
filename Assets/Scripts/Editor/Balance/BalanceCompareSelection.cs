using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareSelection
    {
        public static void Bind(VisualElement grid, BalanceEditorController controller)
        {
            grid.focusable = true;
            if (controller.TakeCompareFocus()) grid.schedule.Execute(grid.Focus);
            grid.RegisterCallback<KeyDownEvent>(evt =>
            {
                for (VisualElement element = evt.target as VisualElement; element != grid; element = element.parent)
                {
                    if (element is Button || element.ClassListContains("unity-base-field")) return;
                }

                if (evt.ctrlKey && evt.keyCode == KeyCode.A)
                {
                    controller.SelectAllCompared(Order(grid));
                    Update(grid, controller.State);
                }
                else if (evt.keyCode == KeyCode.Delete && controller.State.CompareSelection.Count > 0)
                {
                    controller.RemoveSelectedPins();
                    Update(grid, controller.State);
                }
                else return;

                evt.StopPropagation();
                evt.PreventDefault();
            });
        }

        public static void Select(VisualElement grid, BalanceEditorController controller, string id, bool additive, bool range)
        {
            controller.SelectCompared(Order(grid), id, additive, range);
            Update(grid, controller.State);
        }

        public static void Update(VisualElement grid, BalanceWindowState state)
        {
            foreach (VisualElement card in grid.Query<VisualElement>(className: "balance-compare-card").ToList())
                card.EnableInClassList("balance-compare-selected", state.CompareSelection.Contains((string)card.userData));
        }

        private static List<string> Order(VisualElement grid) => grid.Children().Select(slot => (string)slot.userData).ToList();
    }
}
