using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Selection;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public class SelectionFacade: IEntitySelectionFacade, IEntitySelectionObserverFacade, ISelectionPositionProvider
    {
        private readonly ISelectionComponent _selectionComponent;

        public ISelectionModelObserver Selection { get; }
        public SelectionType SelectionType { get; set; }
        public Vector3 WorldPosition => _selectionComponent.WorldPosition;

        public SelectionFacade(ISelectionComponent selectionComponent, ISelectionModelObserver selection,
            SelectionType selectionType)
        {
            Selection = selection;
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
