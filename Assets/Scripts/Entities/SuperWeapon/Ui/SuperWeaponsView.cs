using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public sealed class SuperWeaponsView : MonoBehaviour, ISuperWeaponsView, ITooltipHoverView
    {
        [SerializeField] private List<SuperWeaponButton> buttons;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Button orbitalButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject popup;
        [SerializeField] private GameObject readyIndicator;
        [SerializeField] private GameObject targetingIndicator;
        private readonly HashSet<SuperWeaponType> _readyWeapons = new();
        public TooltipHoverView TooltipHover => tooltipHover;

        public event Action<SuperWeaponType> Pressed;
        public event Action ToggleRequested;
        public event Action CloseRequested;

        public void Initialize()
        {
            orbitalButton.onClick.AddListener(HandleToggle);
            closeButton.onClick.AddListener(HandleClose);
            SetOpen(false);
            foreach (SuperWeaponButton button in buttons)
            {
                button.Initialize();
                button.Pressed += HandlePressed;
            }
            SetPending(null);
        }

        public void Dispose()
        {
            orbitalButton.onClick.RemoveListener(HandleToggle);
            closeButton.onClick.RemoveListener(HandleClose);
            SetOpen(false);
            foreach (SuperWeaponButton button in buttons)
            {
                button.Pressed -= HandlePressed;
                button.Dispose();
            }
        }

        public void SetState(SuperWeaponType type, SuperWeaponState state)
        {
            if (state == SuperWeaponState.Ready) _readyWeapons.Add(type);
            else _readyWeapons.Remove(type);
            readyIndicator.SetActive(_readyWeapons.Count > 0);
            foreach (SuperWeaponButton button in buttons)
                if (button.WeaponType == type) button.SetState(state);
        }

        public void SetPending(SuperWeaponType? type)
        {
            targetingIndicator.SetActive(type.HasValue);
            foreach (SuperWeaponButton button in buttons)
                button.SetPending(type == button.WeaponType);
        }

        private void HandlePressed(SuperWeaponType type) => Pressed?.Invoke(type);
        private void HandleToggle() => ToggleRequested();
        private void HandleClose() => CloseRequested();
        public void SetOpen(bool open) => popup.SetActive(open);
        public void SetBattleAvailable(bool available) => orbitalButton.interactable = available;
        public void SetRemaining(SuperWeaponType type, float seconds)
        {
            foreach (SuperWeaponButton button in buttons)
                if (button.WeaponType == type) button.SetRemaining(seconds);
        }
    }
}
