using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareDrag
    {
        private const float DRAG_THRESHOLD = 4f;

        public static void Bind(VisualElement handle, VisualElement slot, VisualElement grid, BalanceWindowState state, Action refresh, Action<bool> select)
        {
            int pointer = -1;
            Vector2 start = default;
            bool dragging = false;
            VisualElement destination = null;
            bool after = false;
            void ClearDestination()
            {
                if (destination == null) return;
                destination.RemoveFromClassList("balance-drop-before");
                destination.RemoveFromClassList("balance-drop-after");
                destination = null;
            }
            void Reset()
            {
                int captured = pointer;
                pointer = -1; dragging = false;
                ClearDestination();
                slot.RemoveFromClassList("balance-dragging");
                if (captured >= 0 && handle.HasPointerCapture(captured)) handle.ReleasePointer(captured);
            }
            void FindDestination(Vector2 position)
            {
                ClearDestination();
                destination = grid.Children().FirstOrDefault(child => child != slot && child.worldBound.Contains(position));
                if (destination == null) return;
                after = position.x >= destination.worldBound.center.x;
                destination.AddToClassList(after ? "balance-drop-after" : "balance-drop-before");
            }
            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                for (VisualElement element = evt.target as VisualElement; element != handle; element = element.parent)
                    if (element is Button || element.ClassListContains("unity-base-field")) return;
                select(evt.ctrlKey);
                pointer = evt.pointerId; start = evt.position;
                handle.Focus(); handle.CapturePointer(pointer);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.pointerId != pointer) return;
                if (!dragging && Vector2.Distance(start, evt.position) < DRAG_THRESHOLD) return;
                dragging = true; slot.AddToClassList("balance-dragging");
                FindDestination(evt.position);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId != pointer || evt.button != 0) return;
                bool moved = false;
                if (dragging)
                {
                    FindDestination(evt.position);
                    if (destination != null)
                        moved = BalanceCompareOrder.Move(state, (string)slot.userData, (string)destination.userData, after);
                }
                Reset(); evt.StopPropagation();
                if (moved) refresh();
            });
            handle.RegisterCallback<PointerCaptureOutEvent>(_ => Reset());
            handle.RegisterCallback<PointerCancelEvent>(_ => Reset());
            handle.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Escape) return;
                Reset(); evt.StopPropagation();
            });
            handle.RegisterCallback<DetachFromPanelEvent>(_ => Reset());
        }
    }
}
