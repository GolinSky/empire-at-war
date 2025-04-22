using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.ViewComponents.Selection;
using UnityEngine;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionService : IService, INotifier<ISelectionSubject>
    {
        void UpdateSelectable(ISelectable selectable, SelectionType selectionType);
        void RemoveSelectable(ISelectable selectable);
    }

    public class SelectionService : Service, ISelectionService, IInitializable, ILateDisposable, ISelectionSubject
    {

        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;
        private readonly List<IObserver<ISelectionSubject>> _observers = new List<IObserver<ISelectionSubject>>();
      
        private readonly SelectionContext _playerSelectionContext = new SelectionContext();
        private readonly SelectionContext _enemySelectionContext = new SelectionContext();


        public ISelectionContext PlayerSelectionContext => _playerSelectionContext;
        public ISelectionContext EnemySelectionContext => _enemySelectionContext;
        public PlayerType UpdatedType { get; private set; }

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
            RemoveSelectable(selectable.PlayerType);
            switch (selectable.PlayerType)
            {
                case PlayerType.Player:
                {
                    UpdateContext(_playerSelectionContext);
                    break;
                }
                case PlayerType.Opponent:
                {
                    UpdateContext(_enemySelectionContext);
                    break;
                }
            }
            NotifyObservers(selectable.PlayerType);


            void UpdateContext(SelectionContext selectionContext)
            {
                selectionContext.Update(selectable, selectionType);
                selectionContext.SetSelectableState(true);
            }
        }

        public void RemoveSelectable(ISelectable selectable)
        {
            RemoveSelectable(selectable.PlayerType);
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
        
        private void RemoveSelectable(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player:
                    _playerSelectionContext.ResetCurrentSelectable();
                    break;
                case PlayerType.Opponent:
                    _enemySelectionContext.ResetCurrentSelectable();
                    break;
            }
            NotifyObservers(playerType);
        }
        
        private void NotifyObservers(PlayerType playerType)
        {
            UpdatedType = playerType;
            for (var i = 0; i < _observers.Count; i++)
            {
                _observers[i].UpdateState(this);
            }
        }

        public void AddObserver(IObserver<ISelectionSubject> observer)
        {
            _observers.Add(observer);
        }

        public void RemoveObserver(IObserver<ISelectionSubject> observer)
        {
            _observers.Remove(observer);
        }
    }
}