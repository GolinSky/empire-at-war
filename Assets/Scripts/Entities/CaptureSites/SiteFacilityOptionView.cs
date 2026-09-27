using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.CaptureSites
{
    /// <summary>One facility card in a capture site's build choice.</summary>
    public sealed class SiteFacilityOptionView : MonoBehaviour
    {
        [SerializeField] private SiteFacilityType facilityType;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;

        public event Action<SiteFacilityType> Pressed;

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
        }

        public void SetInteractable(bool isInteractable)
        {
            button.interactable = isInteractable;
        }

        private void HandleClicked()
        {
            Pressed?.Invoke(facilityType);
        }
    }
}
