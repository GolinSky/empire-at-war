using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public sealed class FactionProductionMenus : MonoBehaviour
    {
        [SerializeField] private Button researchButton;
        [SerializeField] private Button structuresButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform popup;
        [SerializeField] private RectTransform researchCards;
        [SerializeField] private RectTransform structureCards;
        [SerializeField] private TMP_Text title;

        public RectTransform ResearchCards => researchCards;
        public RectTransform StructureCards => structureCards;
        public bool IsOpen => popup.gameObject.activeSelf;
        public event Action<bool> OpenChanged;

        public void Initialize()
        {
            researchButton.onClick.AddListener(ToggleResearch);
            structuresButton.onClick.AddListener(ToggleStructures);
            closeButton.onClick.AddListener(Close);
            SetAvailable(false);
        }

        public void Dispose()
        {
            researchButton.onClick.RemoveListener(ToggleResearch);
            structuresButton.onClick.RemoveListener(ToggleStructures);
            closeButton.onClick.RemoveListener(Close);
            Close();
        }

        public void SetPopupParent(Transform parent) => popup.SetParent(parent, false);

        public void SetAvailable(bool available)
        {
            researchButton.gameObject.SetActive(available);
            structuresButton.gameObject.SetActive(available);
            if (!available) Close();
        }

        public void DismissOutside(Vector2 position)
        {
            if (IsOpen && !RectTransformUtility.RectangleContainsScreenPoint(popup, position) &&
                !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)researchButton.transform, position) &&
                !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)structuresButton.transform, position))
                Close();
        }

        public void Close()
        {
            if (!IsOpen) return;
            popup.gameObject.SetActive(false);
            OpenChanged?.Invoke(false);
        }

        private void ToggleResearch() => Toggle(true);
        private void ToggleStructures() => Toggle(false);

        private void Toggle(bool research)
        {
            if (IsOpen && research == researchCards.gameObject.activeSelf)
            {
                Close();
                return;
            }

            title.text = research ? "Research" : "Structures";
            researchCards.gameObject.SetActive(research);
            structureCards.gameObject.SetActive(!research);
            popup.SetAsLastSibling();
            if (IsOpen) return;
            popup.gameObject.SetActive(true);
            OpenChanged?.Invoke(true);
        }

        private void OnDestroy()
        {
            if (popup != null && !popup.IsChildOf(transform))
                Destroy(popup.gameObject);
        }
    }
}
