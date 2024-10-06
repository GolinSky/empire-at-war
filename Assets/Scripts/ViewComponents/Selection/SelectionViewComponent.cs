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
        private ISelectionCommand selectionCommand;
        
        protected override void OnInit() {}

        protected override void OnRelease()
        {
            selectionCommand?.OnSkipSelection(selectionType);
            selectionCommand = null;
        }

        public void OnSelected()
        {
            selectionCommand?.OnSelected(selectionType);
        }
    }
}