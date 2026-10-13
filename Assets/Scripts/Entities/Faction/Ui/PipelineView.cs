using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Controllers.Factions;
using System;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public class PipelineView : MonoBehaviour
    {
        [SerializeField] private Image fillIcon;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private Button skipButton;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        private Action<string> _cancelBuilding;

        private string _id;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;
        public bool IsBusy { get; private set; }

        private void Awake()
        {
            skipButton.onClick.AddListener(SkipSequence);
        }

        public void Dispose()
        {
            skipButton.onClick.RemoveListener(SkipSequence);
            _cancelBuilding = null;
        }

        private void OnDestroy() => Dispose();

        private void SkipSequence()
        {
            _cancelBuilding(_id);
        }

        public void Render(ProductionQueueSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            _id = snapshot.UnitRequest.Id;
            tooltipTrigger.SetKey(_id);
            icon.sprite = snapshot.UnitRequest.FactionData.Icon;
            countText.text = snapshot.Count.ToString();
            tierText.text = snapshot.UnitRequest is ResearchUnitRequest research ? $"T{research.Tier}" : "";
            int buildTime = snapshot.UnitRequest.FactionData.BuildTime;
            fillIcon.fillAmount = buildTime > 0
                ? 1f - Mathf.Clamp01(snapshot.RemainingBuildTime / buildTime)
                : 1f;
        }

        public void Activate(bool isActive)
        {
            gameObject.SetActive(isActive);
            IsBusy = isActive;
        }

        public void Init(Action<string> cancelBuilding)
        {
            skipButton.onClick.RemoveListener(SkipSequence);
            skipButton.onClick.AddListener(SkipSequence);
            _cancelBuilding = cancelBuilding ??
                throw new ArgumentNullException(nameof(cancelBuilding));
        }
    }
}
