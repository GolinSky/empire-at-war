using EmpireAtWar.Models.ShipUi;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Views
{
    public interface IShipUi
    {
        void SetModel(IShipUiModelObserver model);
        void SetPresenter(IShipUiPresenter presenter);
        void SetParent(Transform parent);
        void Show();
        void Hide();
        void Initialize();
        void Dispose();
        void SetAbilitySlots(IReadOnlyList<ShipAbilitySlot> slots);
        void SetHealth(IHealthModelObserver health);
    }
}
