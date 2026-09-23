using System.Collections.Generic;
using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views
{
    public class ShipGroupUi : BaseUi<IShipUiModelObserver, IShipUiCommand>,
        IInitializable, ILateDisposable
    {
        [SerializeField] private ShipSelectionGroupUi groupPrefab;

        private readonly List<ShipSelectionGroupUi> _groups = new List<ShipSelectionGroupUi>();
        private bool _hasMovableSelection;
        private bool _isRouteActive = true;

        public void Initialize()
        {
            if (groupPrefab == null)
            {
                throw new System.InvalidOperationException($"{nameof(ShipGroupUi)} requires a group prefab.");
            }

            Model.OnSelectionChanged += HandleChangedSelection;
        }

        public void LateDispose()
        {
            Model.OnSelectionChanged -= HandleChangedSelection;
            ClearGroups();
        }

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
            group.Configure(shipType, icon, amount, visibleEntries, Command.SelectShipGroup);
            group.gameObject.SetActive(_isRouteActive && _hasMovableSelection);
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

        private void HandleChangedSelection(bool hasMovableSelection)
        {
            _hasMovableSelection = hasMovableSelection;
            UpdateVisibility();
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
            bool isVisible = _isRouteActive && _hasMovableSelection;
            gameObject.SetActive(isVisible);
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].gameObject.SetActive(isVisible);
            }
        }
    }
}
