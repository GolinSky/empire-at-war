using EmpireAtWar.Controllers.Factions;
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
        private IFactionView factionView;
        private UnitRequest unitRequest;
        public FactionData FactionData { get; private set; }
        public int Level { get; private set; }


        public void SetData(FactionData factionData, IFactionView factionView, UnitRequest unitRequest)
        {
            FactionData = factionData;
            unitIconImage.sprite = factionData.Icon;
            unitNameText.text = factionData.Name;
            unitPriceText.text = factionData.Price.ToString();
            purchaseButton.onClick.AddListener(HandleClick);
            Level = factionData.AvailableLevel;
            this.factionView = factionView;
            this.unitRequest = unitRequest;
        }

        private void OnDestroy()
        {
            purchaseButton.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            factionView.BuyUnit(unitRequest);
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}
