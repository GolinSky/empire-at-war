using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionService : IService, INotifier<ISelectionSubject>
    {
        ISelectionContext PlayerSelectionContext { get; }
        ISelectionContext OtherSelectionContext { get; }

        void RemoveSelectable(ISelectionContext selectionContext);

        void SelectEntity(IEntity entity);

        void SelectCurrentUnitsByType(UnitTypeId unitTypeId);
    }
}
