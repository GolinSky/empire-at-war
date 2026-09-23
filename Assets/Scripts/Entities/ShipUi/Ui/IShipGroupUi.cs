using EmpireAtWar.Models.Factions;
using System.Collections.Generic;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.ShipUi;
using UnityEngine;

namespace EmpireAtWar.Views
{
    public interface IShipGroupUi
    {
        void SetModel(IShipUiModelObserver model);
        void SetPresenter(IShipUiPresenter presenter);
        void SetParent(Transform parent);
        void Show();
        void Hide();
        void Initialize();
        void Dispose();
        void SetAbilitySlots(IReadOnlyList<ShipAbilitySlot> slots);
        void ClearGroups();
        void AddGroup(ShipType shipType, Sprite icon, int amount, int visibleEntries);
    }
}
