using System;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public sealed class CaptureSiteBuildUi : BaseUi
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private SiteFacilityOptionView[] facilityOptions;
        private CaptureSiteData _data;
        private ICaptureSiteBuildPresenter _presenter;
        private bool _isInitialized;
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
            foreach (SiteFacilityOptionView option in facilityOptions)
            {
                SiteFacilityCost cost = _data.GetCost(option.FacilityType);
                option.Configure(cost.Name.ToUpperInvariant(), $"{cost.Price:0} CR   {cost.BuildTime:0}s");
                option.Pressed += _presenter.Build;
                tooltipHover.Register(option.TooltipTrigger);
            }
            _isInitialized = true;
            Hide();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            foreach (SiteFacilityOptionView option in facilityOptions)
            {
                option.Pressed -= _presenter.Build;
                tooltipHover.Unregister(option.TooltipTrigger);
            }
            _isInitialized = false;
        }

        public void SetPosition(Vector2 screenPosition)
        {
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
            foreach (SiteFacilityOptionView option in facilityOptions)
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
