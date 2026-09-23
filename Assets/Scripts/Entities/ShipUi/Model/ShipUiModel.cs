using System;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.ShipUi
{
    public class ShipUiModel : PureModel, IShipUiModelObserver
    {
        private readonly ShipUiData _data;

        public event Action OnSelectionChanged;

        public bool HasShips { get; private set; }
        public ShipType? SelectedShipType { get; private set; }

        public ShipUiModel(ShipUiData data)
        {
            _data = data;
        }

        public Sprite GetShipIcon(ShipType shipType) => _data.GetShipIcon(shipType);

        public void UpdateSelection(bool hasShips, ShipType? selectedShipType)
        {
            HasShips = hasShips;
            SelectedShipType = selectedShipType;
            OnSelectionChanged?.Invoke();
        }
    }
}
