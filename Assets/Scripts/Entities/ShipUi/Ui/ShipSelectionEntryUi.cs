using System;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views
{
    public sealed class ShipSelectionEntryUi : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image shipIconImage;
        [SerializeField] private TMP_Text amountText;

        private ShipType _shipType;
        private Action<ShipType> _onClicked;

        private void Awake()
        {
            if (button == null || shipIconImage == null || amountText == null)
            {
                throw new InvalidOperationException($"{nameof(ShipSelectionEntryUi)} has an unassigned UI reference.");
            }

            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        public void Configure(ShipType shipType, Sprite icon, int amount, Action<ShipType> onClicked)
        {
            _shipType = shipType;
            _onClicked = onClicked;
            shipIconImage.sprite = icon;
            amountText.gameObject.SetActive(amount > 4);
            amountText.text = amount > 4 ? amount.ToString() : string.Empty;
        }

        private void HandleClick()
        {
            _onClicked(_shipType);
        }
    }
}
