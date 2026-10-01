using EmpireAtWar.Entities.Units;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionService : IService, INotifier<ISelectionSubject>
    {
        ISelectionContext PlayerSelectionContext { get; }
        ISelectionContext OtherSelectionContext { get; }
        void RemoveSelectable(ISelectionContext selectionContext);
        void SelectCurrentUnitsByType(UnitTypeId unitTypeId);
    }
}
