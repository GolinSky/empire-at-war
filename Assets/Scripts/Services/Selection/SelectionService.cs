using System;
using System.Collections.Generic;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.ViewComponents.Selection;
using UnityEngine;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionService : IService, INotifier<ISelectionContext>
    {
        event Action<RaycastHit> OnHitSelected;
        void UpdateSelectable(ISelectable selectable, SelectionType selectionType);
        void RemoveSelectable();
    }

    public class SelectionService : Service, ISelectionService, IInitializable, ILateDisposable
    {
        public event Action<RaycastHit> OnHitSelected;

        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;
        private readonly List<IObserver<ISelectionContext>> _observers = new List<IObserver<ISelectionContext>>();
      
        private readonly SelectionContext _selectionContext = new SelectionContext();

        public SelectionService(IInputService inputService, ICameraService cameraService)
        {
            _inputService = inputService;
            _cameraService = cameraService;
        }
        
        public void Initialize()
        {
            _inputService.OnInput += HandleInput;
        }

        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
        }
        
        public void UpdateSelectable(ISelectable selectable, SelectionType selectionType)
        {
            // switch on player type 
            
            if (_selectionContext.Selectable != null)
            {
                RemoveSelectable();
            }
          
            UpdateSelectionContext(selectable, selectionType);
            _selectionContext.SetActive(true);
            NotifyObservers();
        }
        
        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if(inputType != InputType.Selection) return;
            
            RaycastHit raycastHit = _cameraService.ScreenPointToRay(touchPosition);

            if(raycastHit.collider == null) return;
            
            ISelectableView selectableView = raycastHit.collider.GetComponent<ISelectableView>();

            if (selectableView != null)
            {
                selectableView.OnSelected();
            }
        }

        public void RemoveSelectable()
        {
            _selectionContext.SetActive(false);
            UpdateSelectionContext(null, SelectionType.None);
            NotifyObservers();
        }

        private void UpdateSelectionContext(ISelectable selectable, SelectionType selectionType)
        {
            _selectionContext.SelectionType = selectionType;
            if (selectable != null)
            {
                _selectionContext.Selectable = selectable;
            }
        }

        private void NotifyObservers()
        {
            for (var i = 0; i < _observers.Count; i++)
            {
                _observers[i].UpdateState(_selectionContext);
            }
        }

        public void AddObserver(IObserver<ISelectionContext> observer)
        {
            _observers.Add(observer);
        }

        public void RemoveObserver(IObserver<ISelectionContext> observer)
        {
            _observers.Remove(observer);
        }
    }
}