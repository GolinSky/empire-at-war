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
        }

        private void LayoutOptions()
        {
            Vector2 available = ((RectTransform)panel.parent).rect.size;
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

        public void SetPosition(Vector2 screenPosition)
        {
            LayoutOptions();
            RectTransform parent = (RectTransform)panel.parent;
            _screenPosition = screenPosition;
            _lastParentSize = parent.rect.size;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null,
                out Vector2 position);
            Rect bounds = parent.rect;
            Vector2 size = Vector2.Scale(panel.rect.size, panel.localScale);
            position.x = Mathf.Clamp(position.x, bounds.xMin + size.x * .5f, bounds.xMax - size.x * .5f);
            position.y = Mathf.Clamp(position.y + 24f, bounds.yMin, bounds.yMax - size.y);
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
            if (IsVisible && ((RectTransform)panel.parent).rect.size != _lastParentSize)
                SetPosition(_screenPosition);
        }
    }
}
