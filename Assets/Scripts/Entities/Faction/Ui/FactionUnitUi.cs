using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Factions
{
    public class FactionUnitUi : MonoBehaviour
    {
        [SerializeField] private TooltipTrigger tooltipTrigger;
        public TooltipTrigger TooltipTrigger => tooltipTrigger;
        public void SetAvailable(bool available) => purchaseButton.interactable = available;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI unitPriceText;
        [SerializeField] private Image unitIconImage;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private GameObject queuedHighlight;
        public string RequestId => _unitRequest.Id;

        public void RenderAvailability(bool unlocked, bool affordable, bool queued, bool battleEnded)
        {
            purchaseButton.interactable = unlocked && affordable && !battleEnded;
            unitIconImage.color = new Color(1f, 1f, 1f, unlocked ? 1f : 0.25f);
            unitPriceText.color = affordable ? new Color32(215, 236, 247, 255) : new Color32(239, 107, 92, 255);
            stateText.text = !unlocked ? $"LV {Level}" : !affordable ? "CREDITS" : queued ? "QUEUED" : "";
            queuedHighlight.SetActive(queued);
        }
        private IFactionView _factionView;
        private UnitRequest _unitRequest;
        public FactionData FactionData { get; private set; }
        public int Level { get; private set; }


        public void SetData(FactionData factionData, IFactionView factionView, UnitRequest unitRequest)
        {
            FactionData = factionData;
            unitIconImage.sprite = factionData.Icon;
            unitNameText.text = factionData.Name.ToUpperInvariant();
            unitPriceText.text = factionData.Price.ToString();
            purchaseButton.onClick.AddListener(HandleClick);
            Level = factionData.AvailableLevel;
            _factionView = factionView;
            _unitRequest = unitRequest;
            tooltipTrigger.SetKey(unitRequest);
        }

        private void OnDestroy()
        {
            purchaseButton.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            _factionView.BuyUnit(_unitRequest);
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}
