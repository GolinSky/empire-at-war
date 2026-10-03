using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.UnitActions.Ui
{
    public sealed class UnitActionsView : MonoBehaviour, IUnitActionsView, ITooltipHoverView
    {
        [SerializeField] private List<UnitActionButton> buttons;
        [SerializeField] private TooltipHoverView tooltipHover;

        public event Action<UnitActionId> ActionPressed;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            foreach (UnitActionButton button in buttons)
            {
                button.Initialize();
                button.Pressed += HandlePressed;
            }
            SetPending(null);
        }

        public void Dispose()
        {
            foreach (UnitActionButton button in buttons)
            {
                button.Pressed -= HandlePressed;
                button.Dispose();
            }
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void SetAvailable(UnitActionId action, bool available)
        {
            foreach (UnitActionButton button in buttons)
                if (button.ActionId == action) button.SetAvailable(available);
        }

        public void SetPending(UnitActionId? action)
        {
            foreach (UnitActionButton button in buttons)
                button.SetPending(action == button.ActionId);
        }

        private void HandlePressed(UnitActionId action) => ActionPressed?.Invoke(action);
    }
}
