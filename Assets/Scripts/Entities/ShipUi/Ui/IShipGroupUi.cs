using EmpireAtWar.Models.Factions;
using System;
using System.Collections.Generic;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Services.ShipAbilities;
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
        void ClearGroups();
        void AddGroup(ShipType shipType, IReadOnlyList<ShipUiEntry> ships,
            Action<ShipAbilityId> pressAbility);
    }
}
