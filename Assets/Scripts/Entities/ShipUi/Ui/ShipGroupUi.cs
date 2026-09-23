using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Views
{
    public class ShipGroupUi : BaseUi, IShipGroupUi
    {
        [SerializeField] private ShipSelectionGroupUi groupPrefab;

        private readonly List<ShipSelectionGroupUi> _groups = new List<ShipSelectionGroupUi>();
        private IShipUiModelObserver _model;
        private IShipUiPresenter _presenter;
        private bool _isInitialized;
        private bool _isRouteActive = true;

        public void SetModel(IShipUiModelObserver model) => _model = model;
        public void SetPresenter(IShipUiPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
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
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].gameObject.SetActive(false);
                Destroy(_groups[i].gameObject);
            }
            _groups.Clear();
        }

        public void AddGroup(ShipType shipType, Sprite icon, int amount, int visibleEntries)
        {
            ShipSelectionGroupUi group = Instantiate(groupPrefab, transform.parent);
            group.Configure(shipType, icon, amount, visibleEntries, _presenter.SelectShipGroup);
            group.gameObject.SetActive(_isRouteActive && _model.HasShips);
            _groups.Add(group);
        }

        public override void SetParent(Transform parent)
        {
            base.SetParent(parent);
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].transform.SetParent(parent, false);
            }
        }

        public override void Show()
        {
            _isRouteActive = true;
            base.Show();
            UpdateVisibility();
        }

        public override void Hide()
        {
            _isRouteActive = false;
            base.Hide();
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            bool isVisible = _isRouteActive && _model.HasShips;
            gameObject.SetActive(isVisible);
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].gameObject.SetActive(isVisible);
            }
        }
    }
}
