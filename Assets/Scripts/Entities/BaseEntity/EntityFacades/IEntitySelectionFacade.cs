using EmpireAtWar.Services.Selection;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IEntitySelectionFacade : IEntityFacade
    {
        SelectionType SelectionType { get; set; }

        void Select(bool isSelected);
    }
}
