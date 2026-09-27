using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityFacades.Selection
{
    public class SelectionFacade: IEntitySelectionFacade, ISelectionPositionProvider
    {
        private readonly ISelectionComponent _selectionComponent;
        public SelectionType SelectionType { get; set; }
        public Vector3 WorldPosition => _selectionComponent.WorldPosition;


        public SelectionFacade(SelectionType selectionType, ISelectionComponent selectionComponent)
        {
            _selectionComponent = selectionComponent;
            SelectionType = selectionType;
        }
        
        public void Select(bool isSelected)
        {
            _selectionComponent.SetActive(isSelected);
            // invoke selection components -> last one change model - model invokes event - view updates visuals
        }
    }
}
