using EmpireAtWar.Components.Ui.Tooltip;
using System;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Factions
{
    public class PipelineView : MonoBehaviour
    {
        [SerializeField] private Image fillIcon;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Button skipButton;
        [SerializeField] private TooltipTrigger tooltipTrigger;
        public TooltipTrigger TooltipTrigger => tooltipTrigger;
        
        private Action<string> _cancelBuilding;
        private string _id;
        public bool IsBusy { get; private set; }

        private void Awake()
        {
            skipButton.onClick.AddListener(SkipSequence);
        }

        private void OnDestroy()
        {
            skipButton.onClick.RemoveListener(SkipSequence);
        }

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
            int buildTime = snapshot.UnitRequest.FactionData.BuildTime;
            fillIcon.fillAmount = buildTime > 0
                ? 1f - Mathf.Clamp01(snapshot.RemainingBuildTime / buildTime)
                : 1f;
            progressText.text = $"{Mathf.FloorToInt(fillIcon.fillAmount * 100f)}%";
        }

        public void Activate(bool isActive)
        {
            gameObject.SetActive(isActive);
            IsBusy = isActive;
        }

        public void Init(Action<string> cancelBuilding)
        {
            _cancelBuilding = cancelBuilding ??
                throw new ArgumentNullException(nameof(cancelBuilding));
        }
    }
}
