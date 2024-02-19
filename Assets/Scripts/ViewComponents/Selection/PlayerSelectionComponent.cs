using EmpireAtWar.Models.Selection;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Selection
{
    public class PlayerSelectionComponent:SelectionViewComponent
    {
        [SerializeField] private Canvas selectedCanvas;

        private ISelectionModelObserver modelObserver;
        
        protected override void OnInit()
        {
            base.OnInit();
            modelObserver = ModelObserver.GetModelObserver<ISelectionModelObserver>();
            modelObserver.OnSelected += OnSelected;
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            modelObserver.OnSelected -= OnSelected;
            OnSelected(false);
        }

        private void OnSelected(bool isActive)
        {
            selectedCanvas.enabled = isActive;
        }
    }
}