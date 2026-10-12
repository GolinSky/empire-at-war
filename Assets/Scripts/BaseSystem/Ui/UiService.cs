using System;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Ui.Base
{
    public interface IUiService
    {
        BaseUi CreateUi(UiType uiType);

        void SetHudVisible(bool isVisible);
        void SetContentLayout(bool isFaction, bool isGroup);
        void SetContentVisible(bool isVisible);
    }
    
    public class UiService : MonoBehaviour, IUiService, IHudVisibilityObserver
    {
        [SerializeField] private Canvas defaultCanvas;
        [SerializeField] private Canvas dynamicCanvas;
        [SerializeField] private Canvas popupCanvas;
        [SerializeField] private Canvas feedbackCanvas;
        [SerializeField] private Transform miniMapSlot;
        [SerializeField] private Transform buildPipelineSlot;
        [SerializeField] private Transform economySlot;
        [SerializeField] private UiContentLayout factionLayout;
        [SerializeField] private UiContentLayout shipLayout;
        [SerializeField] private Transform contextualSlot;
        [SerializeField] private Transform endGameSlot;
        [SerializeField] private Transform modalSlot;
        [SerializeField] private Transform tooltipSlot;
        [SerializeField] private Transform fadeSlot;
        private UiFactory _uiFacade;
        private bool _isFactionContent;
        private bool _isContentVisible;

        public bool IsHudVisible { get; private set; } = true;

        [Inject]
        public void Constructor(UiFactory uiFacade) => _uiFacade = uiFacade;

        public BaseUi CreateUi(UiType uiType)
        {
            Transform parent = GetPlacement(uiType);
            BaseUi ui = _uiFacade.Create(uiType, parent);
            ui.SetParent(parent);
            if (uiType == UiType.CoreGame)
            {
                ui.transform.SetAsFirstSibling();
                ((EmpireAtWar.Views.Game.CoreGameUi)ui).ContextualPopup.SetParent(contextualSlot, false);
            }
            if (uiType == UiType.Faction)
            {
                var faction = (EmpireAtWar.Views.Factions.FactionUi)ui;
                faction.SetContentLayout(factionLayout);
            }
            return ui;
        }

        internal Transform GetPlacement(UiType uiType) => uiType switch
        {
            UiType.CoreGame or UiType.MainMenu or UiType.Skirmish or UiType.Reinforcement
                or UiType.Hero => defaultCanvas.transform,
            UiType.Economy => economySlot,
            UiType.Faction => factionLayout.Viewport,
            UiType.Ship or UiType.ShipGroup => shipLayout.Content,
            UiType.MiniMap => miniMapSlot,
            UiType.ShipBuild => buildPipelineSlot,
            UiType.Fps => dynamicCanvas.transform,
            UiType.UnitOrderFeedback => feedbackCanvas.transform,
            UiType.CaptureSiteBuild => contextualSlot,
            UiType.EndGame => endGameSlot,
            UiType.PauseMenu or UiType.Settings => modalSlot,
            UiType.Tooltip => tooltipSlot,
            UiType.Fade => fadeSlot,
            _ => throw new ArgumentOutOfRangeException(nameof(uiType), uiType, "No UI placement is registered.")
        };

        public void SetContentLayout(bool isFaction, bool isGroup)
        {
            _isFactionContent = isFaction;
            factionLayout.SetLayout(true, false);
            shipLayout.SetLayout(false, isGroup);
            SetContentVisible(_isContentVisible);
        }

        public void SetContentVisible(bool isVisible)
        {
            _isContentVisible = isVisible;
            factionLayout.gameObject.SetActive(isVisible && _isFactionContent);
            shipLayout.gameObject.SetActive(isVisible && !_isFactionContent);
        }

        public void SetHudVisible(bool isVisible)
        {
            IsHudVisible = isVisible;
            defaultCanvas.enabled = isVisible;
            dynamicCanvas.enabled = isVisible;
            feedbackCanvas.enabled = isVisible;
            contextualSlot.gameObject.SetActive(isVisible);
        }
    }
}
