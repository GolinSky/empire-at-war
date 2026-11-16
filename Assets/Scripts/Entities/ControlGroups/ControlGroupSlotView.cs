using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.ControlGroups
{
    public sealed class ControlGroupSlotView : MonoBehaviour
    {
        private const float HIGHLIGHT_DURATION = 0.6f;
        private static readonly Color NORMAL_COLOR = new Color32(109, 139, 168, 255);
        private static readonly Color SELECTED_COLOR = new Color32(210, 169, 94, 255);

        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject emptyMark;
        [SerializeField] private Image firstIcon;
        [SerializeField] private Image secondIcon;
        [SerializeField] private GameObject selectionMark;
        [SerializeField] private GameObject emptyOutline;
        [SerializeField] private Graphic[] outline;

        private bool _selected;
        private float _highlightUntil;
        private bool _highlighting;

        public void Render(int count, Sprite first, Sprite second, bool selected)
        {
            countText.text = count == 0 ? string.Empty : count.ToString();
            emptyMark.SetActive(count == 0);
            emptyOutline.SetActive(count == 0);
            foreach (Graphic graphic in outline) graphic.enabled = count > 0;
            firstIcon.sprite = first;
            firstIcon.enabled = count > 0 && first != null;
            secondIcon.sprite = second;
            secondIcon.enabled = count > 0 && second != null;
            _selected = selected;
            selectionMark.SetActive(selected);
            RefreshOutline();
        }

        public void Highlight()
        {
            _highlightUntil = Time.unscaledTime + HIGHLIGHT_DURATION;
            _highlighting = true;
            RefreshOutline();
        }

        private void Update()
        {
            if (!_highlighting || Time.unscaledTime < _highlightUntil) return;
            _highlighting = false;
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            Color color = _selected || _highlighting ? SELECTED_COLOR : NORMAL_COLOR;
            foreach (Graphic graphic in outline) graphic.color = color;
        }
    }
}
