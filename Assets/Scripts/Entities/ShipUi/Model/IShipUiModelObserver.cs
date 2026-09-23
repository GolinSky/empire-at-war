using System;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.ShipUi
{
    public interface IShipUiModelObserver : IModelObserver
    {
        event Action OnSelectionChanged;
        bool HasShips { get; }
        ShipType? SelectedShipType { get; }
        Sprite GetShipIcon(ShipType shipType);
    }
}
