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
        
        private Action<RepublicShipType> clickCallback;

        public FactionData FactionData { get; private set; }
        public RepublicShipType RepublicShipType { get; private set; }


        public void SetData(FactionData factionData, RepublicShipType republicShipType, Action<RepublicShipType> clickCallback)
        {
            RepublicShipType = republicShipType;
            FactionData = factionData;
            this.clickCallback = clickCallback;
            unitIconImage.sprite = factionData.Icon;
            unitNameText.text = factionData.Name;
            unitPriceText.text = factionData.Price.ToString();
            purchaseButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            purchaseButton.onClick.RemoveListener(HandleClick);
            clickCallback = null;
        }

        private void HandleClick()
        {
            clickCallback?.Invoke(RepublicShipType);
        }
    }
}
