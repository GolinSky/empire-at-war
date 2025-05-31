using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionSubject
    {
        ISelectionContext PlayerSelectionContext { get; }
        ISelectionContext EnemySelectionContext { get; }
        PlayerType UpdatedType { get;  }
    }
    
    public interface ISelectionContext
    {
        IEntity Entity { get; }
        IEntitySelectionCommand SelectionCommand { get; }
        SelectionType SelectionType { get; }
        bool HasSelectable { get; }
        PlayerType PlayerType { get; }
    }

    public class SelectionContext: ISelectionContext
    {
        public IEntity Entity { get; private set; }
        public IEntitySelectionCommand SelectionCommand { get; private set; }
        public SelectionType SelectionType { get; private set; }
        public bool HasSelectable => Entity != null;
        public PlayerType PlayerType { get; private set; }

        public void ResetCurrentSelectable()
        {
            SetSelectableState(false);
            Entity = null;
            SelectionType = SelectionType.None;
        }
        
        public void SetSelectableState(bool isSelected) => SelectionCommand?.Select(isSelected);

        public void Update(
            IEntity entity,
            IEntitySelectionCommand selectionCommand,
            SelectionType selectionType,
            PlayerType playerType)
        {
            Entity = entity;
            SelectionCommand = selectionCommand;
            SelectionType = selectionType;
            PlayerType = playerType;
        }
    }

}