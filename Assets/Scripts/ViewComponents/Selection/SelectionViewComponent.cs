using EmpireAtWar.Commands;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using LightWeightFramework.Components.ViewComponents;
using Zenject;

namespace EmpireAtWar.ViewComponents.Selection
{
    public class SelectionViewComponent:ViewComponent
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