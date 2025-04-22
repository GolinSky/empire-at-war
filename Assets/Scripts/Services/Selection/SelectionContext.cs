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
        ISelectable Selectable { get; }
        SelectionType SelectionType { get; }
        bool HasSelectable { get; }
    }

    public class SelectionContext: ISelectionContext
    {
        public ISelectable Selectable { get; private set; }
        public SelectionType SelectionType { get; private set; }
        public bool HasSelectable => Selectable != null;

        public void ResetCurrentSelectable()
        {
            SetSelectableState(false);
            Selectable = null;
            SelectionType = SelectionType.None;
        }
        
        public void SetSelectableState(bool isSelected) => Selectable?.SetActive(isSelected);

        public void Update(ISelectable selectable, SelectionType selectionType)
        {
            Selectable = selectable;
            SelectionType = selectionType;
        }
    }

}