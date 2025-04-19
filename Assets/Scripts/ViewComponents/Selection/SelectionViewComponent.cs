using EmpireAtWar.Commands;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.ViewComponents.Selection
{

    public interface ISelectableView
    {
        void OnSelected();
    }
    
    public class SelectionViewComponent:ViewComponent<ISelectionModelObserver>, ISelectableView
    {
        [SerializeField] private SelectionType selectionType;
        
        [Inject]
        private ISelectionCommand _selectionCommand;
        
        protected override void OnInit() {}

        protected override void OnRelease()
        {
            _selectionCommand?.OnSkipSelection(selectionType);
            _selectionCommand = null;
        }

        public void OnSelected()
        {
            _selectionCommand?.OnSelected(selectionType);
        }
    }
}