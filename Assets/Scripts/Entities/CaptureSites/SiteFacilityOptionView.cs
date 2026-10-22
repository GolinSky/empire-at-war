using EmpireAtWar.Components.Ui.Tooltip;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.CaptureSites
{
    /// <summary>One facility card in a capture site's build choice.</summary>
    public sealed class SiteFacilityOptionView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TooltipTrigger tooltipTrigger;

        [SerializeField] private SiteFacilityType facilityType;

        public event Action<SiteFacilityType> Pressed;

        public TooltipTrigger TooltipTrigger => tooltipTrigger;

        public SiteFacilityType FacilityType => facilityType;
        public string DisplayName => nameText.text;

        private void Awake()
        {
            button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClicked);
        }

        public void Configure(string displayName, string costLabel)
        {
            nameText.text = displayName;
            costText.text = costLabel;
            tooltipTrigger.SetKey(facilityType);
        }

        public void SetInteractable(bool isInteractable)
        {
            // Unaffordable cards remain clickable so the presenter can explain rejection audibly.
            costText.color = isInteractable ? new Color32(215, 236, 247, 255) : new Color32(239, 107, 92, 255);
        }

        private void HandleClicked()
        {
            Pressed?.Invoke(facilityType);
        }
    }
}
