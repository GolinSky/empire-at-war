using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public sealed class CaptureSiteBuildUi : BaseUi
    {
        private const float CARD_WIDTH = 280f;
        private const float CARD_HEIGHT = 360f;
        // Canvas units between the structure's screen point and the panel's near edge and bottom.
        private const float SIDE_OFFSET = 64f;
        private const float VERTICAL_OFFSET = 24f;

        [SerializeField] private RectTransform panel;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private SiteFacilityOptionView[] facilityOptions;
        private readonly List<SiteFacilityOptionView> _options = new List<SiteFacilityOptionView>();
        private CaptureSiteData _data;
        private ICaptureSiteBuildPresenter _presenter;
        private bool _isInitialized;
        private int _visibleOptionCount;
        private Vector3 _panelScale;
        private Vector2 _screenPosition;
        private Vector2 _lastParentSize;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void SetData(CaptureSiteData data) => _data = data;
        public void AssignPresenter(ICaptureSiteBuildPresenter presenter)
        {
            if (_presenter != null)
                throw new InvalidOperationException("CaptureSiteBuildUi presenter can only be assigned once.");
            _presenter = presenter;
        }

        public override void Show()
        {
            transform.SetAsLastSibling();
            base.Show();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            if (_options.Count == 0)
            {
                _options.AddRange(facilityOptions);
                _panelScale = panel.localScale;
            }
            foreach (SiteFacilityOptionView option in _options)
            {
                option.Pressed += _presenter.Build;
                tooltipHover.Register(option.TooltipTrigger);
            }
            _isInitialized = true;
            Hide();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            foreach (SiteFacilityOptionView option in _options)
            {
                option.Pressed -= _presenter.Build;
                tooltipHover.Unregister(option.TooltipTrigger);
            }
            _isInitialized = false;
        }

        public void SetFacilities(IReadOnlyList<SiteFacilityType> types)
        {
            while (_options.Count < types.Count)
            {
                SiteFacilityOptionView option = Instantiate(facilityOptions[0], facilityOptions[0].transform.parent);
                option.Pressed += _presenter.Build;
                tooltipHover.Register(option.TooltipTrigger);
                _options.Add(option);
            }
            _visibleOptionCount = types.Count;
            for (int i = 0; i < _options.Count; i++)
            {
                SiteFacilityOptionView option = _options[i];
                option.gameObject.SetActive(i < types.Count);
                if (i >= types.Count) continue;
                option.Configure(types[i], _data.GetCost(types[i]));
            }
            LayoutOptions();
            ApplyPosition();
        }

        private void LayoutOptions()
        {
            Vector2 available = ((RectTransform)panel.parent).rect.size;
            _lastParentSize = available;
            int columns = Mathf.Clamp(Mathf.FloorToInt(available.x / (CARD_WIDTH * _panelScale.x)),
                1, _visibleOptionCount);
            int rows = Mathf.CeilToInt((float)_visibleOptionCount / columns);
            Vector2 size = new Vector2(columns * CARD_WIDTH, rows * CARD_HEIGHT);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            float scale = Mathf.Min(1f, available.x / (size.x * _panelScale.x),
                available.y / (size.y * _panelScale.y));
            panel.localScale = _panelScale * scale;
            for (int i = 0; i < _visibleOptionCount; i++)
            {
                int column = i % columns;
                int row = i / columns;
                RectTransform rect = (RectTransform)_options[i].transform;
                rect.anchorMin = new Vector2((column + 0.06f) / columns, 1f - (row + 0.95f) / rows);
                rect.anchorMax = new Vector2((column + 0.97f) / columns, 1f - (row + 0.05f) / rows);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        /// <summary>Places the panel above the structure's screen point, to its right or, if that does not fit, its left.</summary>
        public void SetPosition(Vector2 screenPosition)
        {
            if (screenPosition == _screenPosition) return;
            _screenPosition = screenPosition;
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            RectTransform parent = (RectTransform)panel.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, _screenPosition, null,
                out Vector2 anchor);
            Rect bounds = parent.rect;
            Vector2 size = Vector2.Scale(panel.rect.size, panel.localScale);
            float halfWidth = size.x * .5f;
            float x = anchor.x + SIDE_OFFSET + halfWidth;
            if (x + halfWidth > bounds.xMax) x = anchor.x - SIDE_OFFSET - halfWidth;
            Vector2 position;
            position.x = Mathf.Clamp(x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
            position.y = Mathf.Clamp(anchor.y + VERTICAL_OFFSET, bounds.yMin, bounds.yMax - size.y);
            panel.anchoredPosition = position;
        }

        public void SetAvailable(SiteFacilityType type, bool available)
        {
            foreach (SiteFacilityOptionView option in _options)
                if (option.FacilityType == type) option.SetInteractable(available);
        }

        private void OnDestroy() => Dispose();

        private void LateUpdate()
        {
            if (!IsVisible || ((RectTransform)panel.parent).rect.size == _lastParentSize) return;
            LayoutOptions();
            ApplyPosition();
        }
    }
}
