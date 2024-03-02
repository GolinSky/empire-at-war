using System;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Factions
{
    public class FactionUnitUi : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI unitPriceText;
        [SerializeField] private Image unitIconImage;
        [SerializeField] private Button purchaseButton;
        
        private Action<ShipType> clickCallback;

        public FactionData FactionData { get; private set; }
        public ShipType ShipType { get; private set; }
        public int Level { get; private set; }


        public void SetData(FactionData factionData, ShipType shipType, Action<ShipType> clickCallback)
        {
            ShipType = shipType;
            FactionData = factionData;
            this.clickCallback = clickCallback;
            unitIconImage.sprite = factionData.Icon;
            unitNameText.text = factionData.Name;
            unitPriceText.text = factionData.Price.ToString();
            purchaseButton.onClick.AddListener(HandleClick);
            Level = factionData.AvailableLevel;
        }

        private void OnDestroy()
        {
            purchaseButton.onClick.RemoveListener(HandleClick);
            clickCallback = null;
        }

        private void HandleClick()
        {
            clickCallback?.Invoke(ShipType);
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}
