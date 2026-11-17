using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ui;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using MPUIKIT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public class FactionUnitUi : MonoBehaviour
    {
        private const float CLASS_FRAME_WIDTH = 2f;

        private FactionUi _factionView;

        [SerializeField] private TooltipTrigger tooltipTrigger;
        [SerializeField] private TextMeshProUGUI unitPriceText;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private Image unitIconImage;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private MPImage cardBackground;
        [SerializeField] private UiHighlight tutorialHighlight;
        [SerializeField] private GameObject queuedHighlight;
        private UnitRequest _unitRequest;
        private (bool unlocked, bool affordable, bool queued, bool ended, bool atLimit)? _availability;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;
        public UiHighlight TutorialHighlight => tutorialHighlight;
        public string RequestId => _unitRequest.Id;
        public UnitRequest Request => _unitRequest;
        public FactionData FactionData { get; private set; }

        public void SetClassFrame(ShipClass shipClass)
        {
            if (shipClass == ShipClass.Structure) return;

            cardBackground.OutlineColor = shipClass switch
            {
                ShipClass.Fighter or ShipClass.Interceptor or ShipClass.Bomber or ShipClass.HeroStrikecraft =>
                    new Color32(222, 223, 218, 255),
                ShipClass.Corvette => new Color32(141, 182, 153, 255),
                ShipClass.Frigate => new Color32(134, 172, 213, 255),
                ShipClass.Cruiser => new Color32(180, 160, 209, 255),
                ShipClass.HeavyCruiser => new Color32(196, 146, 193, 255),
                ShipClass.Capital => new Color32(214, 164, 121, 255),
                ShipClass.BattleCruiser => new Color32(199, 133, 138, 255),
                ShipClass.Dreadnought => new Color32(212, 102, 108, 255),
                _ => throw new ArgumentOutOfRangeException(nameof(shipClass))
            };
            cardBackground.OutlineWidth = CLASS_FRAME_WIDTH;
        }

        public void RenderAvailability(bool unlocked, bool affordable, bool queued, bool battleEnded,
            bool atLimit = false)
        {
            var availability = (unlocked, affordable, queued, battleEnded, atLimit);
            if (_availability == availability) return;
            _availability = availability;
            purchaseButton.interactable = unlocked && !battleEnded && !atLimit;
            unitIconImage.color = new Color(1f, 1f, 1f, unlocked ? 1f : 0.25f);
            unitPriceText.color = affordable ? new Color32(242, 240, 230, 255) : new Color32(232, 110, 108, 255);
            queuedHighlight.SetActive(queued);
        }

        public void SetData(FactionData factionData, FactionUi factionView, UnitRequest unitRequest)
        {
            FactionData = factionData;
            unitIconImage.sprite = factionData.Icon;
            unitPriceText.text = factionData.Price.ToString();
            unitNameText.text = factionData.DisplayName;
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
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }
}
