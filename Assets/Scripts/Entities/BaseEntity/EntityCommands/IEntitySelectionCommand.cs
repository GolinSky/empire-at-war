using EmpireAtWar.Services.NavigationService;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface IEntitySelectionCommand : IEntityCommand
    {
        void Select(bool isSelected);
        SelectionType SelectionType { get; set; }
    }
}