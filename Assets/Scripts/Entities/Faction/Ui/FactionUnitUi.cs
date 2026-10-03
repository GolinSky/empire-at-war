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
        private IFactionView _factionView;

        [SerializeField] private TooltipTrigger tooltipTrigger;
        [SerializeField] private TextMeshProUGUI unitPriceText;
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private Image unitIconImage;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private GameObject queuedHighlight;
        private UnitRequest _unitRequest;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;
        public string RequestId => _unitRequest.Id;
        public FactionData FactionData { get; private set; }

        public void SetAvailable(bool available) => purchaseButton.interactable = available;

        public void RenderAvailability(bool unlocked, bool affordable, bool queued, bool battleEnded)
        {
            purchaseButton.interactable = unlocked && affordable && !battleEnded;
            unitIconImage.color = new Color(1f, 1f, 1f, unlocked ? 1f : 0.25f);
            unitPriceText.color = affordable ? new Color32(215, 236, 247, 255) : new Color32(239, 107, 92, 255);
            stateText.text = unlocked && queued ? "QUEUED" : "";
            queuedHighlight.SetActive(queued);
        }

        public void SetData(FactionData factionData, IFactionView factionView, UnitRequest unitRequest)
        {
            FactionData = factionData;
            unitIconImage.sprite = factionData.Icon;
            unitPriceText.text = factionData.Price.ToString();
            tierText.text = unitRequest is ResearchUnitRequest research ? $"T{research.Tier}" : "";
            purchaseButton.onClick.AddListener(HandleClick);
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
