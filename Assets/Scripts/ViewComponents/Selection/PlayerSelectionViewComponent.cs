using EmpireAtWar.Models.Selection;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Selection
{
    
    //todo: rename to view component
    public class PlayerSelectionViewComponent:SelectionViewComponent
    {
        [SerializeField] private Canvas selectedCanvas;

        private ISelectionModelObserver _modelObserver;
        
        protected override void OnInit()
        {
            base.OnInit();
            _modelObserver = ModelObserver.GetModelObserver<ISelectionModelObserver>();
            _modelObserver.OnSelected += OnSelected;
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            _modelObserver.OnSelected -= OnSelected;
            OnSelected(false);
        }

        private void OnSelected(bool isActive)
        {
            selectedCanvas.enabled = isActive;
        }
    }
}