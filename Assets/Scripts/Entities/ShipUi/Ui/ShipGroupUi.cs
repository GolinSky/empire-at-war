using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using System;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Views
{
    public class ShipGroupUi : BaseUi, IShipGroupUi
    {
        private const float MAX_ICON_SIZE = 220f;
        private const float MIN_ICON_SIZE = 96f;
        private const float GROUP_SPACING = 10f;
        private const float PANEL_PADDING = 8f;

        [SerializeField] private ShipSelectionGroupUi groupPrefab;

        private readonly List<ShipSelectionGroupUi> _groups = new List<ShipSelectionGroupUi>();
        private IShipUiModelObserver _model;
        private IShipUiPresenter _presenter;
        private RectTransform _content;
        private RectTransform _viewport;
        private Vector2 _lastSize;
        private bool _layoutDirty;
        private bool _isInitialized;
        private bool _isRouteActive = true;

        public void SetModel(IShipUiModelObserver model) => _model = model;
        public void SetPresenter(IShipUiPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            SetParent(transform.parent);
            _model.OnSelectionChanged += UpdateVisibility;
            _isInitialized = true;
            UpdateVisibility();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            _model.OnSelectionChanged -= UpdateVisibility;
            ClearGroups();
            _isInitialized = false;
        }

        private void OnDestroy() => Dispose();

        public void ClearGroups()
        {
            if (_groups.Count > 0)
                _content.sizeDelta = new Vector2(0f, _content.sizeDelta.y);
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].gameObject.SetActive(false);
                Destroy(_groups[i].gameObject);
            }
            _groups.Clear();
            _layoutDirty = true;
        }

        public void AddGroup(ShipType shipType, IReadOnlyList<ShipUiEntry> ships,
            Action<ShipAbilityId> pressAbility)
        {
            ShipSelectionGroupUi group = Instantiate(groupPrefab, transform);
            group.Configure(shipType, _model.GetShipIcon(shipType), ships, _model,
                _presenter.SelectShipGroup, pressAbility);
            group.gameObject.SetActive(true);
            _groups.Add(group);
            _layoutDirty = true;
        }

        public override void SetParent(Transform parent)
        {
            base.SetParent(parent);
            _content = (RectTransform)parent;
            _viewport = (RectTransform)parent.parent;
            _layoutDirty = true;
        }

        public override void Show()
        {
            _isRouteActive = true;
            base.Show();
            UpdateVisibility();
            _layoutDirty = true;
        }

        public override void Hide()
        {
            _isRouteActive = false;
            base.Hide();
            UpdateVisibility();
        }

        private void UpdateVisibility() =>
            gameObject.SetActive(_isRouteActive && _model.HasShips);

        private void LateUpdate()
        {
            Vector2 size = _viewport.rect.size;
            if (!_layoutDirty && size == _lastSize) return;
            _lastSize = size;
            _layoutDirty = false;
            if (_groups.Count == 0) return;

            float height = Mathf.Max(1f, size.y - PANEL_PADDING * 2f);
            float iconSize = Mathf.Min(MAX_ICON_SIZE, Mathf.Max(MIN_ICON_SIZE, height - 114f));
            while (iconSize > MIN_ICON_SIZE && GetTotalWidth(iconSize, height) > size.x)
                iconSize = Mathf.Max(MIN_ICON_SIZE, iconSize - 4f);

            float x = PANEL_PADDING;
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].SetLayout(x, iconSize, height);
                x += _groups[i].GetWidth(iconSize, height) + GROUP_SPACING;
            }
            _content.sizeDelta = new Vector2(Mathf.Max(0f,
                x - GROUP_SPACING + PANEL_PADDING - size.x), 0f);
        }

        private float GetTotalWidth(float iconSize, float height)
        {
            float width = PANEL_PADDING * 2f + (_groups.Count - 1) * GROUP_SPACING;
            for (int i = 0; i < _groups.Count; i++)
                width += _groups[i].GetWidth(iconSize, height);
            return width;
        }
    }
}
