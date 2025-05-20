using EmpireAtWar.Commands;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.ViewComponents.Selection
{
    public class SelectionViewComponent:ViewComponent<ISelectionModelObserver>, ISelectableView
    {
        [SerializeField] private SelectionType selectionType;
        [SerializeField] private Canvas selectedCanvas;

        private bool _canBeSelected = true;//todo: move it to view itself and reuse 
        
        [Inject]
        private ISelectionCommand SelectionCommand { get; }
        
        protected override void OnInit()
        {
            base.OnInit();
            Model.OnSelected += HandleSelection;
        }

        protected override void OnRelease()
        {
            SelectionCommand?.OnSkipSelection(selectionType);
            Model.OnSelected -= HandleSelection;
            HandleSelection(false);
            _canBeSelected = false;
        }

        private void HandleSelection(bool isActive)
        {
            selectedCanvas.enabled = isActive;
        }

        public void OnSelected()
        {
            if (_canBeSelected)
            {
                SelectionCommand?.OnSelected(selectionType);
            }
        }
    }
}