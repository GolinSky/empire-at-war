using System;
using EmpireAtWar.Services.Input;
using UnityEngine;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.NavigationService
{
    public interface INavigationService : IService
    {
        event Action<SelectionType> OnTypeChanged;
        SelectionType SelectionType { get; }
        
        ISelectable Selectable { get; }
        void UpdateSelectable(ISelectable selectableObject, SelectionType selectionType);

        void RemoveSelectable(ISelectable selectable);
        void RemoveSelectable();
    }

    public class NavigationService : Service, INavigationService, IInitializable, ILateDisposable
    {
        public event Action<SelectionType> OnTypeChanged;

        private readonly IInputService inputService;
        private IMovable movable;
        
        public ISelectable Selectable { get; private set; }
        public SelectionType SelectionType { get; private set; }

        public NavigationService(IInputService inputService)
        {
            this.inputService = inputService;
        }

        public void Initialize()
        {
            inputService.OnInput += HandleInput;
        }

        public void LateDispose()
        {
            inputService.OnInput -= HandleInput;
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 screenPosition)
        {
            if(inputType != InputType.ShipInput) return;
            if (movable == null) return;

            if (movable.CanMove)
            {
                movable.MoveToPosition(screenPosition);
            }
        }

        public void UpdateSelectable(ISelectable selectableObject, SelectionType selectionType)
        {
            if(inputService.CurrentTouchPhase == TouchPhase.Moved) return;
            if(Selectable != null) return;
            
            if(selectionType == SelectionType.Terrain) return;
            Selectable = selectableObject;
            movable = selectableObject.Movable;
            selectableObject.SetActive(true);
            
            if (SelectionType != selectionType)
            {
                SelectionType = selectionType;
                OnTypeChanged?.Invoke(SelectionType);
            }
        }

        public void RemoveSelectable(ISelectable selectable)
        {
            if (Selectable == selectable)
            {
                Selectable = null;
                OnTypeChanged?.Invoke(SelectionType.None);
            }
        }

        public void RemoveSelectable()
        {
            if (Selectable != null)
            {
                Selectable.SetActive(false);
                Selectable = null;
                movable = null;
                SelectionType = SelectionType.None;
                OnTypeChanged?.Invoke(SelectionType);
            }
        }
    }
}