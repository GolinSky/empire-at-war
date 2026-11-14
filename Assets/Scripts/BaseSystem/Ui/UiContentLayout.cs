using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Ui.Base
{
    public sealed class UiContentLayout : MonoBehaviour
    {
        private const float FACTION_LEFT = 382f;
        private const float SHIP_LEFT = 554f;
        private const float RIGHT_MARGIN = 8f;
        private const float BOTTOM_MARGIN = 22f;
        private const float FACTION_HEIGHT = 312f;
        private const float FACTION_BOTTOM_MARGIN = 10f;
        private const float SHIP_HEIGHT = 244f;

        [SerializeField] private RectTransform panel;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private ContentSizeFitter fitter;
        private bool _hasLayout;
        private bool _isFaction;
        private bool _isGroup;
        private RectTransform _originalContent;
        private ContentSizeFitter _originalFitter;

        public Transform Content => scroll.content;
        public Transform Viewport => scroll.viewport;

        public static void SetPanelLayout(RectTransform target, bool isFaction)
        {
            float left = isFaction ? FACTION_LEFT : SHIP_LEFT;
            target.anchorMin = Vector2.zero;
            target.anchorMax = new Vector2(1f, 0f);
            target.pivot = Vector2.zero;
            target.anchoredPosition = new Vector2(left, isFaction ? FACTION_BOTTOM_MARGIN : BOTTOM_MARGIN);
            target.sizeDelta = new Vector2(-left - RIGHT_MARGIN, isFaction ? FACTION_HEIGHT : SHIP_HEIGHT);
        }

        public void AttachContent(RectTransform content, ContentSizeFitter contentFitter)
        {
            _originalContent = scroll.content;
            _originalFitter = fitter;
            if (scroll.content != content) scroll.content.gameObject.SetActive(false);
            content.SetParent(scroll.viewport, false);
            scroll.content = content;
            fitter = contentFitter;
            _hasLayout = false;
            SetLayout(true, false);
        }

        public void DetachContent()
        {
            if (scroll != null) scroll.content = _originalContent;
            fitter = _originalFitter;
            if (_originalContent != null) _originalContent.gameObject.SetActive(true);
            _hasLayout = false;
        }

        public void SetLayout(bool isFaction, bool isGroup)
        {
            if (_hasLayout && _isFaction == isFaction && _isGroup == isGroup) return;
            _hasLayout = true;
            _isFaction = isFaction;
            _isGroup = isGroup;
            SetPanelLayout(panel, isFaction);
            scroll.viewport.offsetMin = new Vector2(14f, 12f);
            scroll.viewport.offsetMax = new Vector2(-14f, -12f);
            grid.enabled = !isFaction && !isGroup;
            if (!isFaction)
            {
                grid.cellSize = new Vector2(80f, 220f);
                grid.spacing = new Vector2(8f, 6f);
                grid.padding = new RectOffset();
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Vertical;
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                grid.constraintCount = 1;
            }
            fitter.horizontalFit = isFaction || isGroup ? ContentSizeFitter.FitMode.Unconstrained
                : ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            scroll.content.anchorMin = Vector2.zero;
            scroll.content.anchorMax = new Vector2(isFaction || isGroup ? 1f : 0f, 1f);
            scroll.content.pivot = new Vector2(0f, 1f);
            scroll.content.anchoredPosition = Vector2.zero;
            scroll.content.sizeDelta = Vector2.zero;
            scroll.horizontal = !isFaction;
            scroll.vertical = false;
            if (!isFaction) scroll.horizontalNormalizedPosition = 0f;
        }
    }
}
