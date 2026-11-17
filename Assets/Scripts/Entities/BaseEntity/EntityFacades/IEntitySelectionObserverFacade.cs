using EmpireAtWar.Models.Selection;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Read-only selection state of an entity for views that appear only while it is selected.</summary>
    public interface IEntitySelectionObserverFacade : IEntityFacade
    {
        ISelectionModelObserver Selection { get; }
    }
}
