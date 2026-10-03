using EmpireAtWar.Components.Ui.Tooltip;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.Squadrons;
using System;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views
{
    public class ShipGroupUi : BaseUi, IShipGroupUi, ITooltipHoverView
    {
        private const float CARD_WIDTH = 80f;
        private const float GROUP_SPACING = 10f;

        private IShipUiModelObserver _model;
        private IShipIconProvider _icons;
        private IShipUiPresenter _presenter;

        [SerializeField] private ShipSelectionGroupUi groupPrefab;
        [SerializeField] private TooltipHoverView tooltipHover;
        private readonly List<ShipSelectionGroupUi> _groups = new List<ShipSelectionGroupUi>();
        private RectTransform _content;
        private RectTransform _viewport;

        private Vector2 _lastSize;

        private bool _layoutDirty;
        private bool _isInitialized;
        private bool _isRouteActive = true;

        public TooltipHoverView TooltipHover => tooltipHover;

        [Inject]
        public void Construct(IShipIconProvider icons) => _icons = icons;

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

        public void SetModel(IShipUiModelObserver model) => _model = model;

        public void SetPresenter(IShipUiPresenter presenter) => _presenter = presenter;

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
            group.SetTooltipHover(tooltipHover);
            group.Configure(shipType, _icons.GetShipIcon(shipType), ships, _model,
                _presenter.SelectShipGroup, pressAbility);
            group.gameObject.SetActive(true);
            _groups.Add(group);
            _layoutDirty = true;
        }

        public void AddGroup(SquadronType squadronType, IReadOnlyList<ShipUiEntry> squadrons,
            Action<ShipAbilityId> pressAbility)
        {
            ShipSelectionGroupUi group = Instantiate(groupPrefab, transform);
            group.SetTooltipHover(tooltipHover);
            group.Configure(squadronType, _icons.GetSquadronIcon(squadronType), squadrons, _model,
                _presenter.SelectSquadronGroup, pressAbility);
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

            float x = 0f;
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].SetLayout(x, CARD_WIDTH, size.y);
                x += _groups[i].GetWidth(CARD_WIDTH) + GROUP_SPACING;
            }
            _content.sizeDelta = new Vector2(Mathf.Max(0f, x - GROUP_SPACING - size.x), 0f);
        }


    }
}
