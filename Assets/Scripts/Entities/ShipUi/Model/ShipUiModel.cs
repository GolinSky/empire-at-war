using System;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.ShipUi
{
    public class ShipUiModel : Model, IShipUiModelObserver
    {
        public event Action OnSelectionChanged;

        public bool HasShips { get; private set; }
        public ShipType? SelectedShipType { get; private set; }
        public SquadronType? SelectedSquadronType { get; private set; }
        public ShipAbilityId? PendingAbilityId { get; private set; }

        public void SetPendingAbility(ShipAbilityId? id) => PendingAbilityId = id;

        public void UpdateSelection(bool hasShips, ShipType? selectedShipType,
            SquadronType? selectedSquadronType = null)
        {
            HasShips = hasShips;
            SelectedShipType = selectedShipType;
            SelectedSquadronType = selectedSquadronType;
            OnSelectionChanged?.Invoke();
        }
    }
}
