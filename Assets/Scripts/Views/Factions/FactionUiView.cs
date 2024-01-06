using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    public class FactionUiView : View<IFactionModelObserver>
    {
        [SerializeField] private Canvas controlCanvas;

        protected override void OnInitialize()
        {
            HandleSelectionChanged(Model.SelectionType);
            Model.OnSelectionTypeChanged += HandleSelectionChanged;
        }

        protected override void OnDispose()
        {
            Model.OnSelectionTypeChanged -= HandleSelectionChanged;
        }
        
        private void HandleSelectionChanged(SelectionType selectionType)
        {
            controlCanvas.enabled = selectionType != SelectionType.Terrain;
        }
    }
}