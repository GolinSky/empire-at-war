using EmpireAtWar.Services.NavigationService;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionContext
    {
        ISelectable Selectable { get; }
        SelectionType SelectionType { get; }
    }
    
    public class SelectionContext: ISelectionContext
    {
        public ISelectable Selectable { get; set; }
        public SelectionType SelectionType { get; set; }

        public void SetActive(bool isActive)
        {
            Selectable.SetActive(isActive);
        }
    }
}