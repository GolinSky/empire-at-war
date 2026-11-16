using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.SuperWeapons.Ui
{
    public sealed class SuperWeaponsView : MonoBehaviour, ISuperWeaponsView
    {
        [SerializeField] private List<SuperWeaponButton> buttons;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Button orbitalButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject popup;
        [SerializeField] private GameObject readyIndicator;
        [SerializeField] private GameObject targetingIndicator;
        private readonly HashSet<SuperWeaponType> _readyWeapons = new();

        public event Action<SuperWeaponType> Pressed;

        public event Action ToggleRequested;

        public event Action CloseRequested;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void SetAvailable(bool available)
        {
            orbitalButton.gameObject.SetActive(available);
            if (!available)
            {
                SetOpen(false);
                readyIndicator.SetActive(false);
                targetingIndicator.SetActive(false);
            }
        }

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

        public void SetReady(SuperWeaponType type, bool ready)
        {
            if (ready) _readyWeapons.Add(type);
            else _readyWeapons.Remove(type);
            readyIndicator.SetActive(_readyWeapons.Count > 0);
        }

        public void SetRow(SuperWeaponType type, SuperWeaponState state, bool canAct, string detail, float progress)
        {
            foreach (SuperWeaponButton button in buttons)
                if (button.WeaponType == type) button.Render(state, canAct, detail, progress);
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

        public void SetOpen(bool open)
        {
            if (open) popup.transform.SetAsLastSibling();
            popup.SetActive(open);
        }

        public bool ContainsScreenPoint(Vector2 position) =>
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)popup.transform, position, null) ||
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)orbitalButton.transform, position, null);

        public void SetBattleAvailable(bool available) => orbitalButton.interactable = available;
    }
}
