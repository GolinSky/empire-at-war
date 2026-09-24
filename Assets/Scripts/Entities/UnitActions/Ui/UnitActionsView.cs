using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.UnitActions.Ui
{
    public sealed class UnitActionsView : MonoBehaviour, IUnitActionsView
    {
        [SerializeField] private List<UnitActionButton> buttons;
        [SerializeField] private TMP_Text retreatCountdownText;

        public event Action<UnitActionId> ActionPressed;

        public void Initialize()
        {
            foreach (UnitActionButton button in buttons)
            {
                button.Initialize();
                button.Pressed += HandlePressed;
            }
            SetPending(null);
            SetRetreatCountdown(null);
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

        public void SetRetreatCountdown(float? seconds)
        {
            retreatCountdownText.gameObject.SetActive(seconds.HasValue);
            if (seconds.HasValue)
                retreatCountdownText.text = Mathf.CeilToInt(seconds.Value).ToString();
        }

        private void HandlePressed(UnitActionId action) => ActionPressed?.Invoke(action);
    }
}
