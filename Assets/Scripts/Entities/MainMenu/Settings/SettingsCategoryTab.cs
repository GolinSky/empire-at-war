using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public sealed class SettingsCategoryTab : MonoBehaviour
    {
        [SerializeField] private Toggle categoryToggle;
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text indexText;
        [SerializeField] private Graphic selectedIndicator;

        private void OnEnable()
        {
            categoryToggle.onValueChanged.AddListener(Render);
            Render(categoryToggle.isOn);
        }

        private void OnDisable()
        {
            categoryToggle.onValueChanged.RemoveListener(Render);
        }

        private void Render(bool selected)
        {
            panel.alpha = selected ? 1f : 0f;
            panel.interactable = selected;
            panel.blocksRaycasts = selected;
            categoryText.color = selected ? new Color32(215, 236, 247, 255) : new Color32(123, 156, 175, 255);
            indexText.color = selected ? new Color32(50, 200, 240, 255) : new Color32(123, 156, 175, 255);
            selectedIndicator.enabled = selected;
        }
    }
}
