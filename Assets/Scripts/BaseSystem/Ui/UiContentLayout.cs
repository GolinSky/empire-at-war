using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Ui.Base
{
    public sealed class UiContentLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private ContentSizeFitter fitter;
        private bool _hasLayout;
        private bool _isFaction;
        private bool _isGroup;
        private RectTransform _originalContent;
        private GridLayoutGroup _originalGrid;
        private ContentSizeFitter _originalFitter;

        public Transform Content => scroll.content;
        public Transform Viewport => scroll.viewport;

        public void AttachContent(RectTransform content, GridLayoutGroup contentGrid, ContentSizeFitter contentFitter)
        {
            _originalContent = scroll.content;
            _originalGrid = grid;
            _originalFitter = fitter;
            if (scroll.content != content) scroll.content.gameObject.SetActive(false);
            content.SetParent(scroll.viewport, false);
            scroll.content = content;
            grid = contentGrid;
            fitter = contentFitter;
            _hasLayout = false;
            SetLayout(true, false);
        }

        public void DetachContent()
        {
            scroll.content = _originalContent;
            grid = _originalGrid;
            fitter = _originalFitter;
            _originalContent.gameObject.SetActive(true);
            _hasLayout = false;
        }

        public void SetLayout(bool isFaction, bool isGroup)
        {
            if (_hasLayout && _isFaction == isFaction && _isGroup == isGroup) return;
            _hasLayout = true;
            _isFaction = isFaction;
            _isGroup = isGroup;
            panel.sizeDelta = new Vector2(-612f, isFaction ? 274f : 222f);
            scroll.viewport.offsetMin = new Vector2(14f, 12f);
            scroll.viewport.offsetMax = new Vector2(-14f, isFaction ? -42f : -12f);
            grid.enabled = !isGroup;
            grid.cellSize = isFaction ? new Vector2(138f, 106f) : new Vector2(80f, 198f);
            grid.spacing = new Vector2(8f, 6f);
            grid.padding = new RectOffset();
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = isFaction ? 2 : 1;
            fitter.horizontalFit = isGroup ? ContentSizeFitter.FitMode.Unconstrained
                : ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            scroll.content.anchorMin = Vector2.zero;
            scroll.content.anchorMax = new Vector2(isGroup ? 1f : 0f, 1f);
            scroll.content.pivot = new Vector2(0f, 1f);
            scroll.content.anchoredPosition = Vector2.zero;
            scroll.content.sizeDelta = Vector2.zero;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.horizontalNormalizedPosition = 0f;
        }
    }
}
