using EmpireAtWar.Services.NavigationService;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IEntitySelectionFacade : IEntityFacade
    {
        void Select(bool isSelected);
        SelectionType SelectionType { get; set; }
    }
}
