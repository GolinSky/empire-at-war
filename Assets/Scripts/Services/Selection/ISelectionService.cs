using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionService : IService, INotifier<ISelectionSubject>
    {
        ISelectionContext PlayerSelectionContext { get; }
        ISelectionContext OtherSelectionContext { get; }
        void RemoveSelectable(ISelectionContext selectionContext);
        void SelectCurrentShipsByType(ShipType shipType);
        void SelectCurrentSquadronsByType(SquadronType squadronType);
    }
}
