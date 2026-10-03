using System;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.ShipUi
{
    public interface IShipUiModelObserver : IModelObserver
    {
        event Action OnSelectionChanged;

        bool HasShips { get; }
        ShipType? SelectedShipType { get; }
        SquadronType? SelectedSquadronType { get; }
        ShipAbilityId? PendingAbilityId { get; }
    }
}
